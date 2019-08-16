using UnityEngine;
using System;

public class TriggerCallbacks : MonoBehaviour
{
    public Action<Collider> onTriggerEnter;
    public Action<Collider> onTriggerStay;
    public Action<Collider> onTriggerExit;
    void OnTriggerEnter(Collider other) => onTriggerEnter?.Invoke(other);
    void OnTriggerStay(Collider other) => onTriggerStay?.Invoke(other);
    void OnTriggerExit(Collider other) => onTriggerExit?.Invoke(other);
}
