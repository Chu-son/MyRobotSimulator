using UnityEngine;
using System.Collections;

public class ColliderTrigger : MonoBehaviour {
    GameObject parent;

    // Use this for initialization
    void Start()
    {
        parent = gameObject.transform.parent.gameObject;
    }

    void OnTriggerEnter(Collider collider) { parent.SendMessage("RedirectedOnTriggerEnter", collider); }
    void OnTriggerStay(Collider collider) { parent.SendMessage("RedirectedOnTriggerStay", collider); }
    void OnTriggerExit(Collider collider) { parent.SendMessage("RedirectedOnTriggerExit", collider); }

    void OnCollisionEnter(Collision collision) { parent.SendMessage("RedirectedOnCollisionEnter", collision); }
    void OnCollisionStay(Collision collision) { parent.SendMessage("RedirectedOnCollisionStay", collision); }
    void OnCollisionExit(Collision collision) { parent.SendMessage("RedirectedOnCollisionExit", collision); }

    void RedirectedOnTriggerEnter(Collider collider) { parent.SendMessage("RedirectedOnTriggerEnter", collider); }
    void RedirectedOnTriggerStay(Collider collider) { parent.SendMessage("RedirectedOnTriggerStay", collider); }
    void RedirectedOnTriggerExit(Collider collider) { parent.SendMessage("RedirectedOnTriggerExit", collider); }

    void RedirectedOnCollisionEnter(Collision collision) { parent.SendMessage("RedirectedOnCollisionEnter", collision); }
    void RedirectedOnCollisionStay(Collision collision) { parent.SendMessage("RedirectedOnCollisionStay", collision); }
    void RedirectedOnCollisionExit(Collision collision) { parent.SendMessage("RedirectedOnCollisionExit", collision); }
}
