using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Stats : MonoBehaviour {

    public Image mindProgress;
    public Image soulProgress;
    public Image bodyProgress;

    public float mindPercent;
    public float soulPercent;
    public float bodyPercent;

    public Text mindText;
    public Text soulText;
    public Text bodyText;

    float total_mindPercent;
    float total_soulPercent;
    float total_bodyPercent;

    float currProgMind = 0;
    float currProgBody = 0;
    float currProgSoul = 0;

    Sequence sequence;

    public float speed;

    // Use this for initialization
    void Start () {
        ResetFillAmount();
        float total = mindPercent + soulPercent + bodyPercent;
        total_mindPercent = mindPercent / total;
        currProgSoul = total_mindPercent;
        total_soulPercent = (soulPercent + mindPercent) / total;
        currProgBody = total_soulPercent;
        total_bodyPercent = 1f;
        Tween();
    }

    private void ResetFillAmount()
    {
        bodyProgress.fillAmount = 0;
        mindProgress.fillAmount = 0;
        soulProgress.fillAmount = 0;
    }

    void Tween()
    {
        sequence.Append(bodyProgress.DOFillAmount(total_bodyPercent, speed));
        sequence.Append(soulProgress.DOFillAmount(total_soulPercent, speed));
        sequence.Append(mindProgress.DOFillAmount(total_mindPercent, speed));
        sequence.Pause();
    }
    void OnComplete()
    {
        sequence.Rewind();
    }

    private void OnEnable()
    {
        mindText.text = "Mind: " + mindPercent;
        bodyText.text = "Body: " + bodyPercent;
        soulText.text = "Soul: " + soulPercent;

        ResetFillAmount();
        Tween();
        sequence.Pause();
        sequence.Rewind();
        sequence.Play();
    }

    // Update is called once per frame
    void Update ()
    {

        //if ( currProgMind < total_mindPercent )
        //{
        //    currProgMind += speed * Time.deltaTime;
        //}

        //if (currProgBody < total_bodyPercent)
        //{
        //    currProgMind += speed * Time.deltaTime;
        //}

        //if (currProgSoul < total_soulPercent)
        //{
        //    currProgMind += speed * Time.deltaTime;
        //}
        //bodyProgress.fillAmount = currProgBody;
        //mindProgress.fillAmount = currProgMind;
        //soulProgress.fillAmount = currProgSoul;
    }
}

