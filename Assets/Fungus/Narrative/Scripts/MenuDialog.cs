using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Fungus
{
	
	public class MenuDialog : MonoBehaviour
	{
		// Currently active Menu Dialog used to display Menu options
		public static MenuDialog activeMenuDialog;

		protected Button[] cachedButtons;
		protected Slider cachedSlider;

		public static MenuDialog GetMenuDialog()
		{
			if (activeMenuDialog == null)
			{
				// Use first Menu Dialog found in the scene (if any)
				MenuDialog md = GameObject.FindObjectOfType<MenuDialog>();
				if (md != null)
				{
					activeMenuDialog = md;
				}
				
				if (activeMenuDialog == null)
				{
					// Auto spawn a menu dialog object from the prefab
					GameObject prefab = Resources.Load<GameObject>("MenuDialog");
					if (prefab != null)
					{
						GameObject go = Instantiate(prefab) as GameObject;
						go.SetActive(false);
						go.name = "MenuDialog";
						activeMenuDialog = go.GetComponent<MenuDialog>();
					}
				}
			}
			
			return activeMenuDialog;
		}

		public virtual void Awake()
		{
			Button[] optionButtons = GetComponentsInChildren<Button>();
			cachedButtons = optionButtons;

			Slider timeoutSlider = GetComponentInChildren<Slider>();
			cachedSlider = timeoutSlider;

			if (Application.isPlaying)
			{
				// Don't auto disable buttons in the editor
				Clear();
			}
		}

		public virtual void OnEnable()
		{
			// The canvas may fail to update if the menu dialog is enabled in the first game frame.
			// To fix this we just need to force a canvas update when the object is enabled.
			Canvas.ForceUpdateCanvases();
		}

		protected virtual void Clear()
		{
			StopAllCoroutines();

			Button[] optionButtons = GetComponentsInChildren<Button>();						
			foreach (UnityEngine.UI.Button button in optionButtons)
			{
				button.onClick.RemoveAllListeners();
			}
			
			foreach (UnityEngine.UI.Button button in optionButtons)
			{
				if (button != null)
				{
					button.gameObject.SetActive(false);
				}
			}

			Slider timeoutSlider = GetComponentInChildren<Slider>();
			if (timeoutSlider != null)
			{
				timeoutSlider.gameObject.SetActive(false);
			}
		}

		public virtual bool AddOption(string text, Sequence targetSequence)
		{

			bool addedOption = false;
			foreach (Button button in cachedButtons)
			{
				if (!button.gameObject.activeSelf)
				{
					button.gameObject.SetActive(true);

					Text textComponent = button.GetComponentInChildren<Text>();
					if (textComponent != null)
					{
						textComponent.text = text;
					}
					
					Sequence sequence = targetSequence;
					
					button.onClick.AddListener(delegate {

						StopAllCoroutines(); // Stop timeout
						Clear();

						HideSayDialog();

						if (sequence != null)
						{
							#if UNITY_EDITOR
							// Select the new target sequence in the Fungus Script window
							FungusScript fungusScript = sequence.GetFungusScript();
							fungusScript.selectedSequence = sequence;
							#endif

							gameObject.SetActive(false);

							sequence.ExecuteCommand(0);
						}
					});

					addedOption = true;
					break;
				}
			}
			
			return addedOption;
		}

		protected virtual void HideSayDialog()
		{
			SayDialog sayDialog = SayDialog.GetSayDialog();
			if (sayDialog != null)
			{
				bool fadingOut = false;
				bool movingOut = false;
				if (sayDialog.alwaysFadeDialog)
				{
					sayDialog.FadeOutDialog();
					fadingOut = true;
				}
				if (sayDialog.alwaysMoveDialog)
				{
					sayDialog.MoveOutDialog();
					movingOut = true;
				}
				if (!fadingOut && !movingOut)
				{
					sayDialog.ShowDialog(false);
				}
			}
		}

		public virtual void ShowTimer(float duration, Sequence targetSequence)
		{

			if (cachedSlider != null)
			{
				cachedSlider.gameObject.SetActive(true);
				StopAllCoroutines();
				StartCoroutine(WaitForTimeout(duration, targetSequence));
			}
		}

		protected virtual IEnumerator WaitForTimeout(float timeoutDuration, Sequence targetSequence)
		{
			float elapsedTime = 0;
			
			Slider timeoutSlider = GetComponentInChildren<Slider>();
			
			while (elapsedTime < timeoutDuration)
			{
				if (timeoutSlider != null)
				{
					float t = 1f - elapsedTime / timeoutDuration;
					timeoutSlider.value = t;
				}
				
				elapsedTime += Time.deltaTime;
				
				yield return null;
			}
			
			Clear();
			gameObject.SetActive(false);

			HideSayDialog();

			if (targetSequence != null)
			{
				targetSequence.ExecuteCommand(0);
			}
		}
	}
	
}