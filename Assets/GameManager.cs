using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using System;
using NBaseSDK;

public class GameManager : MonoBehaviour
{
    public string username;
    public int maxMessages = 30;
    public float messageSpace = 5.0f;
    public string channelId;
    public NBaseSDK.Chat _nc = NBaseSDK.Chat.GetInstance();
    private Queue<NBaseSDK.Message> messageQueue = new Queue<NBaseSDK.Message>();
    public GameObject chatPanel, textObject;
    public TMP_InputField chatBox;

    [SerializeField]
    public List<Message> messageList = new List<Message>();

    private void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        HandleInput();
        if (messageQueue.Count > 0)
        {
            NBaseSDK.Message message = messageQueue.Dequeue();
            addMessage("[" + message.sender.name + "] " + message.content, Message.MessageType.PlayerMessage);
        }
    }
    void OnDestroy()
    {
        _nc.dispatcher.onMessageReceived -= OnMessageReceived;
        _nc.dispatcher.onMessageDeleted -= OnMessageDeleted;
        _nc.dispatcher.onConnected -= OnConnected;
    }
    public void EnqueueMessage(NBaseSDK.Message message)
    {
        messageQueue.Enqueue(message);
    }
    private void OnMessageReceived(NBaseSDK.Message message)
    {
        EnqueueMessage(message);
    }
    private void OnMessageDeleted(NBaseSDK.Message message)
    {
        EnqueueMessage(message);
    }
    private void OnConnected(User user)
    {
    }
    private async void Initialize()
    {
        try
        {
            _nc.setDebug(true);
            _nc.initialize("339c2b1c-d35b-47f2-828d-5f02a130146a", "alpha", "en");
            _nc.dispatcher.onMessageReceived += OnMessageReceived;
            _nc.dispatcher.onMessageDeleted += OnMessageDeleted;
            _nc.dispatcher.onConnected += OnConnected;
            await Connect();
        }
        catch (InvalidOperationException e)
        {
            Debug.Log("InvalidOperationException: {0}" + e.Message);
        }
        catch (Exception e)
        {
            Debug.Log("Error: {0}" + e.Message);
        }

    }
    private async Task Connect()
    {
        try
        {
            var user = await _nc.Connect(
                   userId: "10001",
                   name: username,
                   profile: "https://random.imagecdn.app/500/150",
                   language: "en");

            await _nc.subscribe(channelId);

            // last messages
            Hashtable filter = new Hashtable
            {
                { "state", true },
                { "channel_id", channelId }
            };
            Hashtable sort = new Hashtable
            {
                { "created_at", -1 },
            };
            Hashtable option = new Hashtable
            {
                { "offset", 0 },
                { "per_page", 10 },
            };
            var messages = await _nc.getMessages(filter, sort, option);
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    addMessage("[" + message.sender.name + "] " + message.content, Message.MessageType.PlayerMessage);
                }
            }
        }
        catch (InvalidOperationException e)
        {
            Debug.LogError("InvalidOperationException: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

    }
    private void HandleInput()
    {
        if (!string.IsNullOrEmpty(chatBox.text) && Input.GetKeyDown(KeyCode.Return))
        {
            sendMessage(chatBox.text);
            chatBox.text = "";
            chatBox.ActivateInputField();
        }
        else if (!chatBox.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                chatBox.ActivateInputField();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                addMessage("You pressed the space bar!", Message.MessageType.Info);
            }
        }
    }
    
    public void sendMessage(string text)
    {
        _nc.sendMessage(channelId, "text", text, null, true);
    }
    public void addMessage(string text, Message.MessageType messageType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogError("Attempted to add an empty message to the chat.");
            return;
        }

        if (textObject == null || chatPanel == null)
        {
            Debug.LogError("Chat UI components not assigned in the inspector!");
            return;
        }

        if (messageList.Count >= maxMessages)
        {
            Destroy(messageList[0].textObject.gameObject);
            messageList.RemoveAt(0);
        }

        try
        {
            GameObject newTextObject = Instantiate(textObject, chatPanel.transform);
            TMP_Text newText = newTextObject.GetComponent<TMP_Text>();
            if (newText == null)
            {
                throw new MissingComponentException("TMP_Text component not found on instantiated text object.");
            }

            newText.text = text;
            // newText.color = MessageTypeColor(messageType); // 메시지 타입에 따른 색상 설정 필요

            Message newMessage = new Message
            {
                text = text,
                textObject = newText
            };
            messageList.Add(newMessage);

            AdjustChatPanelHeight();
        }
        catch (Exception e)
        {
            Debug.LogError("Error adding message: " + e.Message);
        }

    }
    private void AdjustChatPanelHeight()
    {
        float totalHeight = 0;
        foreach (var message in messageList)
        {
            totalHeight += message.textObject.preferredHeight + messageSpace;
        }

        // chatPanel의 높이를 조정하는 로직이 필요하면 여기에 추가
    }
    private Color MessageTypeColor(Message.MessageType messageType)
    {
        switch (messageType)
        {
            case Message.MessageType.PlayerMessage:
                return new Color32(255, 255, 255, 255);
            case Message.MessageType.Info:
                return new Color32(15, 98, 230, 255);
            default:
                return new Color32(255, 255, 255, 255);
        }
    }
    
    //public void setListener()
    //{
    //    _nc.dispatcher.onConnected += e =>
    //    {
    //        addMessage("Connected to server with id: {0} " + e.ToString(), Message.MessageType.Info);
    //    };

    //    _nc.dispatcher.onDisconnected += e =>
    //    {
    //        addMessage("Disconnect", Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onErrorReceived += e =>
    //    {
    //        addMessage("onErrorReceived: " + e.ToString(), Message.MessageType.Info);
    //    };
        
    //    //_nc.dispatcher.onMessageReceived += e =>
    //    //{
    //    //    addMessage("onMessageReceived: " + e.ToString(), Message.MessageType.Info);
    //    //};
    //    _nc.dispatcher.onMessageDeleted += e =>
    //    {
    //        addMessage("onMessageDeleted: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onMemberAdded += e =>
    //    {
    //        addMessage("onMemberAdded: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onMemberLeft += e =>
    //    {
    //        addMessage("nMemberLeft: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onMemberUpdated += e =>
    //    {
    //        addMessage("onMemberUpdated: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onMemberDeleted += e =>
    //    {
    //        addMessage("onMemberDeleted: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onStartTyping += e =>
    //    {
    //        addMessage("onStartTyping: " + e.ToString(), Message.MessageType.Info);
    //    };
    //    _nc.dispatcher.onStopTyping += e =>
    //    {
    //        addMessage("onStopTyping: " + e.ToString(), Message.MessageType.Info);
    //    };
    //}
}

[System.Serializable]
public class Message
{
    public string text;
    public TMP_Text textObject;
    public enum MessageType
    {
        PlayerMessage,
        Info,
    }
}