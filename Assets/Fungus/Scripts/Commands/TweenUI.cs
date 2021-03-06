// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using System.Collections.Generic;
using Fungus;
using Fungus.Variables;

namespace Fungus.Commands
{
    /// <summary>
    /// Abstract base class for TweenUI commands.
    /// </summary>
    public abstract class TweenUI : Command 
    {
        [Tooltip("List of objects to be affected by the tween")]
        [SerializeField] protected List<GameObject> targetObjects = new List<GameObject>();
        
        [Tooltip("Type of tween easing to apply")]
        [SerializeField] protected LeanTweenType tweenType = LeanTweenType.easeOutQuad;
        
        [Tooltip("Wait until this command completes before continuing execution")]
        [SerializeField] protected BooleanData waitUntilFinished = new BooleanData(true);
        
        [Tooltip("Time for the tween to complete")]
        [SerializeField] protected FloatData duration = new FloatData(1f);
        
        public override void OnEnter()
        {
            if (targetObjects.Count == 0)
            {
                Continue();
                return;
            }
            
            ApplyTween();

            if (!waitUntilFinished)
            {
                Continue();
            }
        }
        
        protected virtual void ApplyTween()
        {
            foreach (GameObject targetObject in targetObjects)
            {
                if (targetObject == null)
                {
                    continue;
                }
                
                ApplyTween(targetObject);
            }
            
            if (waitUntilFinished)
            {
                LeanTween.value(gameObject, 0f, 1f, duration).setOnComplete(OnComplete);
            }
        }
        
        protected abstract void ApplyTween(GameObject go);

        protected virtual void OnComplete()
        {
            Continue();
        }

        public override void OnCommandAdded(Block parentBlock)
        {
            // Add an empty slot by default. Saves an unnecessary user click.
            if (targetObjects.Count == 0)
            {
                targetObjects.Add(null);
            }
        }

        protected virtual string GetSummaryValue()
        {
            return "";
        }
        
        public override string GetSummary()
        {
            if (targetObjects.Count == 0)
            {
                return "Error: No targetObjects selected";
            }
            else if (targetObjects.Count == 1)
            {
                if (targetObjects[0] == null)
                {
                    return "Error: No targetObjects selected";
                }
                return targetObjects[0].name + " = " + GetSummaryValue();
            }
            
            string objectList = "";
            foreach (GameObject gameObject in targetObjects)
            {
                if (gameObject == null)
                {
                    continue;
                }
                
                if (objectList == "")
                {
                    objectList += gameObject.name;
                }
                else
                {
                    objectList += ", " + gameObject.name;
                }
            }
            
            return objectList + " = " + GetSummaryValue();
        }
        
        public override Color GetButtonColor()
        {
            return new Color32(180, 250, 250, 255);
        }

        public override bool IsReorderableArray(string propertyName)
        {
            if (propertyName == "targetObjects")
            {
                return true;
            }

            return false;
        }
    }
}