using System;
using UnityEngine;

namespace TK_Shared.ObjectInteractions3D
{
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableObject : MonoBehaviour
    {
        Rigidbody _rb;
        BoxCollider _col;
        
        void Awake()
        {
            _rb=GetComponent<Rigidbody>();
            _col=GetComponent<BoxCollider>();
        }

        public void Grab(Transform objectGrabPivotTransform)
        {
            transform.position = objectGrabPivotTransform.position;
            transform.rotation = objectGrabPivotTransform.rotation;
            transform.parent = objectGrabPivotTransform;
            _rb.useGravity = false;
            _rb.isKinematic = true;
            _col.enabled = false;
        }

        public void Drop()
        {
            transform.parent = null;
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _col.enabled = true;
        }
    }
}
