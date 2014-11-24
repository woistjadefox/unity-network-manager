using UnityEngine;
using System;
using System.Collections.Generic;

namespace Goga.UnityNetwork {

    public enum AnimationParameter {
        Bool, Float
    }

    public class AnimationSynchronizer : MonoBehaviour {

        public Animator animator;

        [System.Serializable]
        public class ParameterElement {
            public string name;
            public AnimationParameter type;
            [HideInInspector]
            public bool boolValue;
            [HideInInspector]
            public float floatValue;
        }

        public ParameterElement[] parameters;

        void Start() {

            // try to get animator from gameobject
            if (animator == null) {
                this.animator = GetComponent<Animator>();
            }
        }

        public void AddParameter(string name, AnimationParameter type) {

            ParameterElement newParameter = new ParameterElement();
            newParameter.name = name;
            newParameter.type = type;
            parameters[parameters.Length] = newParameter;

        }

        public void UpdateValues(BitStream stream) {

            foreach (ParameterElement parm in parameters) {

                if (parm.type == AnimationParameter.Bool) {

                    if (stream.isWriting) {
                        parm.boolValue = animator.GetBool(parm.name);
                        stream.Serialize(ref parm.boolValue);
                    } else {
                        stream.Serialize(ref parm.boolValue);
                        animator.SetBool(parm.name, parm.boolValue);
                    }

                }

                if (parm.type == AnimationParameter.Float) {

                    if (stream.isWriting) {
                        parm.floatValue = animator.GetFloat(parm.name);
                        stream.Serialize(ref parm.floatValue);
                    } else {
                        stream.Serialize(ref parm.floatValue);
                        animator.SetFloat(parm.name, parm.floatValue);
                    }

                }
            }
        }

    }
}
