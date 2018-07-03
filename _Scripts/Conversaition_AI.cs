using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.Assistant.v1;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Connection;
using FullSerializer;
using IBM.Watson.DeveloperCloud.DataTypes;

public class Conversaition_AI : MonoBehaviour
{
    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Space(10)]
    [Tooltip("The service URL (optional). This defaults to \"https://gateway.watsonplatform.net/assistant/api\"")]
    [SerializeField]
    private string _serviceUrl;
    [Tooltip("The workspaceId to run the example.")]
    [SerializeField]
    private string _workspaceId;
    [Tooltip("The version date with which you would like to use the service in the form YYYY-MM-DD.")]
    [SerializeField]
    private string _versionDate;
    [Header("CF Authentication")]
    [Tooltip("The authentication username.")]
    [SerializeField]
    private string _username;
    [Tooltip("The authentication password.")]
    [SerializeField]
    private string _password;
    [Header("IAM Authentication")]
    [Tooltip("The IAM apikey.")]
    [SerializeField]
    private string _iamApikey;
    [Tooltip("The IAM url used to authenticate the apikey (optional). This defaults to \"https://iam.bluemix.net/identity/token\".")]
    [SerializeField]
    #endregion
    private string _iamUrl;
    public Text responceText;
    public Text inputText;
    public AudioSource voiceSource;
    public VoiceType voiceType;
    private Assistant _assistant;
    private Dictionary<string, object> _conversationContext = null;
    private SpeechToText _speechToText;
    private TextToSpeech _textToSpeech;
    private fsSerializer _serializer = new fsSerializer();
    private Dictionary<string, object> _context = null;
    private Assistant _service;
    public AudioClip _audio;
    public AudioClip _recording;
    private int _recordingRoutine = 0;
    private string _microphoneID = null;
    //private AudioClip _recording = null;
    private int _recordingBufferSize = 1;
    private int _recordingHZ = 22050;
    string voiceText;

    public GameObject mainMenu;

    public void OnBtnRecordPressed()
    {
    }
    public void OnBtnRecordReleased()
    {
    }
    private void OnNewResponce(object response, Dictionary<string, object> customData)
    {
        //TODO:@
        //  Convert resp to fsdata
        fsData fsdata = null;
        fsResult r = _serializer.TrySerialize(response.GetType(), response, out fsdata);
        if (!r.Succeeded)
            throw new WatsonException(r.FormattedMessages);

        //  Convert fsdata to MessageResponse
        MessageResponse messageResponse = new MessageResponse();
        object obj = messageResponse;
        r = _serializer.TryDeserialize(fsdata, obj.GetType(), ref obj);
        if (!r.Succeeded)
            throw new WatsonException(r.FormattedMessages);
        //  Set context for next round of messaging
        object _tempContext = null;
        (response as Dictionary<string, object>).TryGetValue("context", out _tempContext);

        if (_tempContext != null)
            _context = _tempContext as Dictionary<string, object>;
        else
            Log.Debug("ExampleAssistant.OnMessage()", "Failed to get context");


        try
        {
            object tempIntentsObj = null;
            (response as Dictionary<string, object>).TryGetValue("intents", out tempIntentsObj);
            object tempIntentObj = (tempIntentsObj as List<object>)[0];
            object tempIntent = null;
            (tempIntentObj as Dictionary<string, object>).TryGetValue("intent", out tempIntent);
            string intent = tempIntent.ToString();

            //  Get text
            object tempTextsObj = null;
            (response as Dictionary<string, object>).TryGetValue("output", out tempTextsObj);
            object tempTextObj = (tempTextsObj as Dictionary<string, object>);
            object temptext = null;
            (tempTextObj as Dictionary<string, object>).TryGetValue("text", out temptext);
            string text = (temptext as List<object>)[0].ToString();//ToString();

            Log.Debug("text", text);
            _textToSpeech.ToSpeech(OnAudioResponce, OnAudioFailed, text);
            inputText.text = text;

            if (intent.Equals("General_Ending") || intent.Equals("Stop_Training"))
            {
                StopRecording();
                Active = false;
                OnFinishedQuest();
            }
        }
        catch (System.Exception ex)
        {
        	
        }
        
    }

    private void OnFinishedQuest()
    {
        mainMenu.SetActive(true);
        inputText.transform.gameObject.SetActive(false);
    }


    private void OnMessageFailed(RESTConnector.Error error, Dictionary<string, object> customData)
    {

    }

    private void OnAudioResponce(AudioClip clip, Dictionary<string, object> customData)
    {
        voiceSource.PlayOneShot(clip);
    }
    private void OnAudioFailed(RESTConnector.Error error, Dictionary<string, object> customData)
    {

    }

    private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData)
    {
        //Log.Debug("ExampleSpeechToText.HandleOnRecognize()", "Speech to Text - Get model response: {0}", customData["json"].ToString());
        if (result != null && result.results.Length > 0)
        {
            foreach (var res in result.results)
            {
                foreach (var alt in res.alternatives)
                {
                    if(res.final == true )
                    {
                        voiceText = alt.transcript;
                        MessageRequest messageRequest = new MessageRequest()
                        {
                            Input = new Dictionary<string, object>()
                            {
                                { "text",  voiceText}
                            }
                        };
                        _service.Message(OnNewResponce, OnMessageFailed,
                            "b886d7ee-7da3-4927-9531-39102c6b632d", messageRequest);
                        voiceText = "";
                    }
                    string text = string.Format("{0} ({1}, {2:0.00})\n", alt.transcript, res.final ? "Final" : "Interim", alt.confidence);
                    Log.Debug("ExampleStreaming.OnRecognize()", text);
                }

                if (res.keywords_result != null && res.keywords_result.keyword != null)
                {
                    foreach (var keyword in res.keywords_result.keyword)
                    {
                        Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                    }
                }

                if (res.word_alternatives != null)
                {
                    foreach (var wordAlternative in res.word_alternatives)
                    {
                        Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                        foreach (var alternative in wordAlternative.alternatives)
                            Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                    }
                }
            }
        }
    }

    private void OnRecognitionFail(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Log.Error("ExampleSpeechToText.OnFail()", "Error received: {0}", error.ToString());
    }

    public void onBtnTestClick()
    {
        MessageRequest messageRequest = new MessageRequest()
        {
            Input = new Dictionary<string, object>()
            {
                { "text",  voiceText}
            }
            
        };
        _service.Message(OnNewResponce, OnMessageFailed, 
            "b886d7ee-7da3-4927-9531-39102c6b632d", messageRequest);
        voiceText = "";
        //_textToSpeech.ToSpeech(OnAudioResponce, OnAudioFailed, "This is your last day. Welcome to the jungle!");
        //_speechToText.Recognize(OnSpeechRecognition, OnRecognitionFail);
    }
  
    private void OnListWorkspaces(WorkspaceCollection response, Dictionary<string, object> customData)
    {
        Log.Debug("ExampleAssistant.OnListWorkspaces()", "Response: {0}", customData["json"].ToString());   
    }

    private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Log.Debug("ExampleAssistant.OnFail()", "Response: {0}", customData["json"].ToString());
        Log.Error("TestAssistant.OnFail()", "Error received: {0}", error.ToString());
    }
    public bool Active
    {
        get { return _speechToText.IsListening; }
        set
        {
            if (value && !_speechToText.IsListening)
            {
                _speechToText.DetectSilence = true;
                _speechToText.EnableWordConfidence = true;
                _speechToText.EnableTimestamps = true;
                _speechToText.SilenceThreshold = 0.1f;
                _speechToText.MaxAlternatives = 0;
                _speechToText.EnableInterimResults = true;
                _speechToText.OnError = OnError;
                _speechToText.InactivityTimeout = -1;
                _speechToText.ProfanityFilter = false;
                _speechToText.SmartFormatting = true;
                _speechToText.SpeakerLabels = false;
                _speechToText.WordAlternativesThreshold = null;
                _speechToText.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && _speechToText.IsListening)
            {
                _speechToText.StopListening();
            }
        }
    }
    private IEnumerator CreateService()
    {
        //  Create credential and instantiate service
        Credentials credentials = null;
        if (true)
        {
            //  Authenticate using username and password
            credentials = new Credentials(
                "c751cdcd-e076-4062-b6b3-4a9a607d3920", 
                "PklYn6YfFt4N",
                "https://gateway.watsonplatform.net/assistant/api");
        }
        else if (!string.IsNullOrEmpty(_iamApikey))
        {
            //  Authenticate using iamApikey
            TokenOptions tokenOptions = new TokenOptions()
            {
                IamApiKey = _iamApikey,
                IamUrl = _iamUrl
            };

            credentials = new Credentials(tokenOptions, _serviceUrl);

            //  Wait for tokendata
            while (!credentials.HasIamTokenData())
                yield return null;
        }
        else
        {
            throw new WatsonException("Please provide either username and password or IAM apikey to authenticate the service.");
        }

        _service = new Assistant(credentials);
        _service.VersionDate = "2018-02-16";
        Credentials txtToSpeech = new Credentials(
            "085bafbf-34ce-45be-9412-6fd11ebc845f",
            "BBHhTeBhntxO",
            "https://stream.watsonplatform.net/text-to-speech/api"
            );
       
        _textToSpeech = new TextToSpeech(txtToSpeech);

        Credentials speechToTxt = new Credentials(
           "691d6c70-b40d-4187-bcf7-4089a41b84d0",
           "s0MOcqLN8AKR",
           "https://stream.watsonplatform.net/speech-to-text/api"
           );
        _speechToText = new SpeechToText(speechToTxt);
        //_speechToText.StreamMultipart = true;

        Active = true;
        StartRecording();
        //Runnable.Run(Examples());
    }
    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }

    private void OnError(string error)
    {
        Active = false;

        Log.Debug("ExampleStreaming.OnError()", "Error! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        Log.Debug("ExampleStreaming.RecordingHandler()", "devices: {0}", Microphone.devices);
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let _recordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("ExampleStreaming.RecordingHandler()", "Microphone disconnected.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
                record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                _speechToText.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }

        }

        yield break;
    }
    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null)
        {
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
            }
        }
    }
    // Use this for initialization
    void Start () {
        LogSystem.InstallDefaultReactors();

        Credentials assist = new Credentials()
        {
            Url = "https://gateway.watsonplatform.net/assistant/api",
            Username = "c751cdcd-e076-4062-b6b3-4a9a607d3920",
            Password = "PklYn6YfFt4N"
        };
        StartCoroutine(CreateService());
    }
	
	// Update is called once per frame
	void Update () {
	}
}



/*
private void StartListening()
{
    _speechToText.DetectSilence = true;
    _speechToText.EnableWordConfidence = false;
    _speechToText.EnableTimestamps = false;
    _speechToText.SilenceThreshold = 0.03f;
    _speechToText.MaxAlternatives = 1;
    _speechToText.StartListening(OnSpeechToTextResultReceived, OnRecognizeSpeaker);
}
private void SendMessageToConversation(string spokenText)
{
    MessageRequest messageRequest = new MessageRequest()
    {
        input = new Dictionary<string, object>()
            {
                { "text", spokenText }
            },
        context = _context
    };

    Debug.Log("Sent to Watson Conversation: " + spokenText);
    if (_conversation.Message(OnConversationMessageReceived, OnFail, WATSON_CONVERSATION_WORKSPACE_ID, messageRequest))
        Log.Debug("Error", "Failed to send message");
}
private void OnConversationMessageReceived(object resp, Dictionary<string, object> customData)
// derived from Watson Unity SDK: https://github.com/watson-developer-cloud/unity-sdk/blob/336ebba141337047fe95ece06e5034fa9818666e/Examples/ServiceExamples/Scripts/ExampleConversation.cs#L118
{
}
private void CallTextToSpeech(string outputText)
{
    Debug.Log("Sent to Watson Text To Speech: " + outputText);
    if (!_textToSpeech.ToSpeech(OnSynthesize, OnFail, outputText, false))
        Log.Debug("ExampleTextToSpeech.ToSpeech()", "Failed to synthesize!");
}
//The callback OnSpeechToTextResultReceived gets the spoken text as input:
private void OnSpeechToTextResultReceived(SpeechRecognitionEvent result, Dictionary<string, object> customData)
{
    if (result != null && result.results.Length > 0)
    {
        foreach (var res in result.results)
        {
            foreach (var alt in res.alternatives)
            {
                SendMessageToConversation(alt.transcript);
            }
        }
    }
}
private void OnSynthesize(AudioClip clip, Dictionary<string, object> customData)
{
    Debug.Log("Received audio file from Watson Text To Speech");

    if (Application.isPlaying && clip != null)
    {
        GameObject audioObject = new GameObject("AudioObject");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.spatialBlend = 0.0f;
        source.loop = false;
        source.clip = clip;
        source.Play();

        Invoke("RecordAgain", source.clip.length);
        Destroy(audioObject, clip.length);
    }
}
private void RecordAgain()
{
    Debug.Log("Played Audio received from Watson Text To Speech");
    if (!_stopListeningFlag)
    {
        OnListen();
    }
}*/