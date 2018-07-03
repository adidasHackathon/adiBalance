using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TrifectaAnimation : MonoBehaviour {


    public RectTransform redImage;
    public RectTransform greenImage;
    public RectTransform blueImage;

    public Vector3 zoomFactor;

    public float squeezeDuration = 0.2f;
    public float animDuration;
    public float zoomDuration = 1f;

    public float squeezePoints = 20f;

    bool bMoved;
    

    Sequence sequence;
    private void Start()
    {
        sequence = DOTween.Sequence();
        sequence.Join(redImage.DOAnchorPos(new Vector2(0, -squeezePoints), squeezeDuration));
        sequence.Join(greenImage.DOAnchorPos(new Vector2(squeezePoints, squeezePoints), squeezeDuration));
        sequence.Join(blueImage.DOAnchorPos(new Vector2(-squeezePoints, squeezePoints), squeezeDuration));


        sequence.Append(redImage.DOAnchorPos(new Vector2(0, 1280f), animDuration));
        sequence.Join(greenImage.DOAnchorPos(new Vector2(-720f, -1280f), animDuration));
        sequence.Join(blueImage.DOAnchorPos(new Vector2(720f, -1280f), animDuration));

        sequence.Join(redImage.DOScale(zoomFactor, zoomDuration));
        sequence.Join(greenImage.DOScale(zoomFactor, zoomDuration));
        sequence.Join(blueImage.DOScale(zoomFactor, zoomDuration));

        sequence.OnComplete(OnAnimComplete);
        sequence.Pause();
        bMoved = true;
    }

    void OnAnimComplete()
    {
        sequence.Rewind();
    }

    // Update is called once per frame
    void Update ()
    {
	    if (Input.touchCount > 0 || Input.GetMouseButtonDown(0) )
        {
            sequence.Play();
            bMoved = true;
        }
        if ( !bMoved )
        {
            redImage.Rotate(new Vector3(0f, 0f, -30f));
            greenImage.Rotate(new Vector3(0f, 0f, -30f));
            blueImage.Rotate(new Vector3(0f, 0f, -30f));
        }
    }
}
