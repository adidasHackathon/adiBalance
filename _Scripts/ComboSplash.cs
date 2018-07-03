using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ComboSplash : MonoBehaviour {

    public float speed = 2.0f;
    private Text text;
    

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        text.DOText("16" + System.Environment.NewLine +
            "days" + System.Environment.NewLine +
            "in" + System.Environment.NewLine +
            "a" + System.Environment.NewLine +
            "row!", speed, true, ScrambleMode.All);
    }
    private void Update()
    {
        if (Input.touchCount > 0 || Input.GetMouseButtonDown(0) )
        {
            gameObject.transform.parent.gameObject.SetActive(false);
        }
    }
}
