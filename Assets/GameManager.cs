using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using System;
using NBaseSDK;
using System.Diagnostics;

public class GameManager : MonoBehaviour
{
    public string username;
    public int maxMessages = 30;
    public float messageSpace = 5.0f;
    public string channelId;
    public NBaseSDK.Chat _nc;
    private Queue<string> messageQueue = new Queue<string>();
    public GameObject chatPanel, textObject;
    public TMP_InputField chatBox;
    public Button[] chatButtons;

    [SerializeField]
    public List<Message> messageList = new List<Message>();

    private void Start()
    {
        _nc = NBaseSDK.Chat.GetInstance();
        Initialize();

        foreach (Button button in chatButtons)
        {
            button.onClick.AddListener(() => OnChatButtonClick(button));
        }
    }

    void Update()
    {
        HandleInput();
        if (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            addMessage(message, Message.MessageType.PlayerMessage);
            messageQueue.Clear();
        }
    }
    void OnDestroy()
    {
        _nc.dispatcher.onMessageReceived -= OnMessageReceived;
        _nc.dispatcher.onMessageDeleted -= OnMessageDeleted;
        _nc.dispatcher.onConnected -= OnConnected;
    }
    public void EnqueueMessage(string message)
    {
        messageQueue.Enqueue(message.ToString());
    }
    private void OnMessageReceived(NBaseSDK.Message message)
    {
        EnqueueMessage(message.content);
    }
    private void OnMessageDeleted(NBaseSDK.Message message)
    {
        EnqueueMessage(message.content);
    }
    private void OnConnected(NBaseSDK.User user)
    {
        EnqueueMessage("OnConnected");
    }
    private async void Initialize()
    {
        try
        {
            _nc.setDebug(true);
            _nc.initialize("339c2b1c-d35b-47f2-828d-5f02a130146a", "alpha", "en");
            setListener();
            await Connect();

        }
        catch (InvalidOperationException e)
        {
            UnityEngine.Debug.Log("InvalidOperationException: " + e.Message);
            addMessage("[Error] InvalidOperationException : " + e.Message, Message.MessageType.PlayerMessage);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Error: " + e.Message);
            addMessage("[Error] : " + e.Message, Message.MessageType.PlayerMessage);
        }

    }
    private async Task Connect()
    {
        try
        {
            var user = await _nc.Connect(
                  userId: "10001",
                  name: username,
                  profile: "https://random.imagecdn.app/500/150");

            await _nc.subscribe(channelId);

            Hashtable filter = new Hashtable
            {
                { "deleted", false },
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
           
            var users = await _nc.getUsers(filter, sort, option);
            if (users != null)
            {
                foreach (var item in users.edges)
                {
                    addMessage("[Users] id" + item.Node.id.ToString(), Message.MessageType.Info);
                }
            }
           
            
            var channels = await _nc.getChannels(filter, sort, option);
            if (channels != null)
            {
                foreach (var channel in channels.edges)
                {
                    addMessage("[Channels] id=" + channel.Node.id.ToString(), Message.MessageType.Info);
                }
            }

            var subscriptions = await _nc.getSubscriptions(filter, sort, option);
            if (subscriptions != null)
            {
                foreach (var subscription in subscriptions.edges)
                {
                    addMessage("[Subscriptions] id=" + subscription.Node.id.ToString(), Message.MessageType.Info);
                }
            }
            // last messages
            //Hashtable filter = new Hashtable
            //{
            //    { "state", true },
            //    { "channel_id", channelId }
            //};
            //Hashtable sort = new Hashtable
            //{
            //    { "created_at", -1 },
            //};
            //Hashtable option = new Hashtable
            //{
            //    { "offset", 0 },
            //    { "per_page", 10 },
            //};
            //var messages = await _nc.getMessages(filter, sort, option);
            //if (messages != null)
            //{
            //    foreach (var message in messages)
            //    {
            //        addMessage("[" + message.sender.name + "] " + message.content, Message.MessageType.PlayerMessage);
            //    }
            //}
        }
        catch (InvalidOperationException e)
        {
            UnityEngine.Debug.Log("InvalidOperationException: " + e.Message);
            addMessage("[Error] InvalidOperationException : " + e.Message, Message.MessageType.PlayerMessage);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Error: " + e.Message);
            addMessage("[Error] Error : " + e.Message, Message.MessageType.PlayerMessage);
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
        try
        {
            _nc.sendMessage(channelId, "text", text, null, true);
        }
        catch (InvalidOperationException e)
        {
            UnityEngine.Debug.Log("InvalidOperationException: " + e.Message);
            addMessage("[Error] InvalidOperationException : " + e.Message, Message.MessageType.PlayerMessage);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Error: " + e.Message);
            addMessage("[Error] Error : " + e.Message, Message.MessageType.PlayerMessage);
        }
    }
    public void addMessage(string text, Message.MessageType messageType)
    {
       
        if (string.IsNullOrWhiteSpace(text))
        {
            UnityEngine.Debug.LogError("Attempted to add an empty message to the chat.");
            return;
        }

        if (textObject == null || chatPanel == null)
        {
            UnityEngine.Debug.LogError("Chat UI components not assigned in the inspector!");
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
            UnityEngine.Debug.LogError("Error adding message: " + e.Message);
            addMessage("[Error] Error adding message : " + e.Message, Message.MessageType.PlayerMessage);
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

    public void setListener()
    {
        _nc.dispatcher.onConnected += e =>
        {
            EnqueueMessage("Connected to server with id: {0} " + e.ToString());
        };

        _nc.dispatcher.onDisconnected += e =>
        {
            EnqueueMessage("Disconnect");
        };
        _nc.dispatcher.onErrorReceived += e =>
        {
            EnqueueMessage("onErrorReceived: " + e.ToString());
        };

        _nc.dispatcher.onMessageReceived += e =>
        {
            EnqueueMessage("onMessageReceived: " + e.content.ToString());
        };
        _nc.dispatcher.onMessageDeleted += e =>
        {
            EnqueueMessage("onMessageDeleted: " + e.ToString());
        };
        _nc.dispatcher.onMemberAdded += e =>
        {
            EnqueueMessage("onMemberAdded: " + e.ToString());
        };
        _nc.dispatcher.onMemberLeft += e =>
        {
            EnqueueMessage("nMemberLeft: " + e.ToString());
        };
        _nc.dispatcher.onMemberUpdated += e =>
        {
            EnqueueMessage("onMemberUpdated: " + e.ToString());
        };
        _nc.dispatcher.onMemberDeleted += e =>
        {
            EnqueueMessage("onMemberDeleted: " + e.ToString());
        };
        _nc.dispatcher.onStartTyping += e =>
        {
            EnqueueMessage("onStartTyping: " + e.ToString());
        };
        _nc.dispatcher.onStopTyping += e =>
        {
            EnqueueMessage("onStopTyping: " + e.ToString());
        };
    }

    void OnChatButtonClick(Button button)
    {
        switch (button.name)
        {
            case "btnSend":
                if (!string.IsNullOrEmpty(chatBox.text))
                {
                    sendMessage(chatBox.text);
                    chatBox.text = "";
                    chatBox.ActivateInputField();
                }
                break;
            default:
                break;
        }
    }
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