using UnityEngine;
using UnityEngine.Events;

public class CallAFunctWhenChildNone : MonoBehaviour
{
    [SerializeField] private UnityEvent onChildCountZero;
    public bool hasTriggered = false;

    void Update()
    {
        if (this.transform.childCount == 0 && !hasTriggered)
        {
            onChildCountZero.Invoke();
            hasTriggered = true;
        }
        else if (this.transform.childCount > 0)
        {
            hasTriggered = false;
        }
    }
}
