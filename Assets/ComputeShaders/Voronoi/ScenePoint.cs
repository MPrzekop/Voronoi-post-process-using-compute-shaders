using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScenePoint : MonoBehaviour
{
    private UnityEvent _onTransformChange;

    public UnityEvent OnTransformChange
    {
        get
        {
            if (_onTransformChange == null)
            {
                _onTransformChange = new UnityEvent();
            }

            return _onTransformChange;
        }
        set => _onTransformChange = value;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            OnTransformChange.Invoke();
            transform.hasChanged = false;
        }
    }
}