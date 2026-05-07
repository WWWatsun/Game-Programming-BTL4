using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class UnoGameManager : NetworkBehaviour
{
    public static UnoGameManager Instance { get; private set; }

    [Header("Network Synced State")]
    // Replaces your standard int/enum variables
    public NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    public NetworkVariable<CardColor> syncedCurrentColor = new NetworkVariable<CardColor>();
    public NetworkVariable<int> syncedCurrentPenalty = new NetworkVariable<int>(0);
    public NetworkVariable<int> topDiscardCardID = new NetworkVariable<int>();

    [Header("Players")]
    [SerializeField] private List<PlayerController> players = new List<PlayerController>();

    [Header("Current Game State")]
    [SerializeField] private CardScriptables topDiscardCard;
    [SerializeField] private CardColor currentColor;

    [Header("Initial Deal")]
    [SerializeField] private bool dealCardsOnStart = true;
    [SerializeField] private int initialCardCount = 7;

    [Header("Local View Debug")] //nhi: gia lap may hien tai la Player0, sau nay sua lại de lam feature multiplayer
    [SerializeField] private int localPlayerIndex = 0;

    [Header("UI Visuals")] // Duoc dung de render top discard card
    [SerializeField] private SpriteRenderer topDiscardCardDisplay;

    [Header("Penalty State")]
    [SerializeField] private int currentPenalty = 0; // Lưu số bài phạt cộng dồn (+2, +4)

    [Header("Rule 8 State")]
    private List<int> rule8Responders = new List<int>();
    private bool isReactionEventActive = false;

    [Header("UI Panels (Custom Rules)")]
    [SerializeField] private GameObject panelRule0; // Panel chọn chiều
    [SerializeField] private GameObject panelRule7; // Panel chọn người để đổi bài
    [SerializeField] private GameObject panelRule8; // Panel nút Reaction
    [SerializeField] private GameObject panelColorPicker; //Panel chọn màu khi dùng lá wild hoặce +4

    [Header("Rule 7 Dynamic UI")] //Tạo Ra nút bấm dựa vào số lượng player
    [SerializeField] private GameObject rule7ButtonPrefab; // Prefab của 1 cái nút
    [SerializeField] private Transform rule7ButtonContainer; // GameObject chứa các nút (có gắn Layout Group)

    // Biến lưu tạm lá Wild/+4 đang chờ người chơi chọn màu
    private CardScriptables pendingWildCard;

    // Biến dùng để ghi nhớ người chốt chuỗi +4
    private PlayerController lastPlus4Player = null;

    public int LocalPlayerIndex => localPlayerIndex;

    private TurnManager turnManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Ẩn tất cả UI lúc mới vào game
        if (panelRule0) panelRule0.SetActive(false);
        if (panelRule7) panelRule7.SetActive(false);
        if (panelRule8) panelRule8.SetActive(false);
        if (panelColorPicker) panelColorPicker.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe using the named methods
        topDiscardCardID.OnValueChanged += OnTopCardChanged;
        currentTurnIndex.OnValueChanged += OnTurnIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe using the exact same named methods
        topDiscardCardID.OnValueChanged -= OnTopCardChanged;
        currentTurnIndex.OnValueChanged -= OnTurnIndexChanged;
    }

    private void OnTopCardChanged(int previousID, int newID)
    {
        // Double-check the ID exists to prevent errors during initialization
        if (Deck.Instance.CardDatabase.ContainsKey(newID))
        {
            CardScriptables newTopCard = Deck.Instance.CardDatabase[newID];
            topDiscardCardDisplay.sprite = newTopCard.cardSprite;
        }
    }

    private void OnTurnIndexChanged(int previousTurn, int newTurn)
    {
        UpdateTurnVisuals();
    }

    private void AutoAssignPlayersIfNeeded()
    {
        // Xóa các slot bị None trước
        players = players.Where(player => player != null).ToList();

        // Nếu list vẫn có player hợp lệ rồi thì không cần tự tìm nữa
        if (players.Count > 0)
        {
            return;
        }

        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        players = foundPlayers
            .OrderBy(player => player.transform.GetSiblingIndex())
            .ToList();

        Debug.Log($"Auto assigned {players.Count} players.");
    }

    private void DealInitialCards()
    {
        // Ensure only the Host deals the cards!
        if (!IsServer) return; 

        for (int cardIndex = 0; cardIndex < initialCardCount; cardIndex++)
        {
            foreach (PlayerController player in players)
            {
                // Host draws the card securely
                CardScriptables card = Deck.Instance.DrawCard();
            
                // Set up the target parameters so ONLY this specific player gets the message
                ClientRpcParams rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { player.OwnerClientId }
                    }
                };

                // Send the card ID over the network to that player
                player.ReceiveCardClientRpc(card.cardID, rpcParams);
            }
        }
    }

    private void InitTopDiscardCardIfNeeded()
    {
        if (topDiscardCard != null)
        {
            currentColor = topDiscardCard.cardColor;
            Deck.Instance.GetDiscarded(topDiscardCard);
            return;
        }

        topDiscardCard = Deck.Instance.DrawCard();

        while (topDiscardCard.cardColor == CardColor.NEUTRAL)
        {
            Deck.Instance.GetDiscarded(topDiscardCard);
            topDiscardCard = Deck.Instance.DrawCard();
        }

        currentColor = topDiscardCard.cardColor;
        Deck.Instance.GetDiscarded(topDiscardCard);

        Debug.Log($"Initial top discard card: {topDiscardCard.CardName()}");
        UpdateTopDiscardDisplay(); // <-- THÊM GỌI HÀM NÀY
    }

    public bool IsLegalMove(PlayerController player, CardScriptables card)
    {
        if (player == null || card == null) return false;

        if (!player.IsMyTurn)
        {
            return false;
        }

        return UnoRuleEngine.IsLegalMove(
            playedCard: card,
            topCard: topDiscardCard,
            currentColor: currentColor,
            playerCardCount: player.Hand.Cards.Count,
            pendingPenalty: currentPenalty
        );
    }

    public void TryPlayCard(PlayerController player, CardScriptables card)
    {
        if (!IsLegalMove(player, card)) return;

        // 1. Server removes the card locally
        player.Hand.RemoveFromHandOnly(card);
        Deck.Instance.GetDiscarded(card);

        // NEW: 2. Server tells THAT SPECIFIC CLIENT to remove it from their screen
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.OwnerClientId } } };
        player.RemoveCardClientRpc(card.cardID, rpcParams);

        // 3. Update the Top Card
        topDiscardCard = card;
        topDiscardCardID.Value = card.cardID; // <-- NEW: This magically updates all clients' screens!

        if (player.Hand.Cards.Count == 0)
        {
            Debug.Log($"Player {player.PlayerIndex} đã đánh hết bài!");
            CheckGameEndCondition(); // Gọi hàm kiểm tra kết thúc
        }

        // 4. Update the Color
        if (card.cardColor != CardColor.NEUTRAL)
        {
            currentColor = card.cardColor;
            syncedCurrentColor.Value = card.cardColor; // <-- NEW: Sync the color across the network
        }

        Debug.Log($"Player {player.PlayerIndex} played {card.CardName()}");

        // KIỂM TRA NẾU LÀ LÁ ĐEN (WILD / +4)
        if (card.cardColor == CardColor.NEUTRAL)
        {
            if (card.cardValue == CardValue.PLUS4)
            {
                lastPlus4Player = player;
                ApplyBasicCardEffect(card);
                UpdateTurnVisuals();
                player.PromptColorSelectionClientRpc(rpcParams);
            }
            else
            {
                pendingWildCard = card;
                player.PromptColorSelectionClientRpc(rpcParams);
            }
        }
    }

    private void CheckGameEndCondition()
    {
        int activePlayersCount = 0;
        PlayerController lastLoser = null;

        // Quét toàn bộ list player xem ai còn bài
        foreach (PlayerController p in players)
        {
            if (p.Hand.Cards.Count > 0)
            {
                activePlayersCount++;
                lastLoser = p;
            }
        }

        // Nếu chỉ còn 1 người duy nhất có bài -> GAME OVER
        if (activePlayersCount <= 1 && lastLoser != null)
        {
            Debug.Log($"<color=red>GAME OVER! Player {lastLoser.PlayerIndex} là người thua cuộc!</color>");
            // (TODO: Hiển thị UI Win/Lose ở đây)

            this.enabled = false; // Tạm dừng mọi thao tác logic của GameManager
        }
    }

    // --- CÁC HÀM XỬ LÝ CHỌN MÀU MỚI ---
    private void ShowColorPickerUI(CardScriptables card)
    {
        pendingWildCard = card;
        panelColorPicker.SetActive(true);
        Debug.Log("Waiting for player to choose a color...");
    }

    public void ApplyColorChoiceOnServer(int colorIndex)
    {
        // Hide UI for the host (just in case they were the one playing it)
        panelColorPicker.SetActive(false);

        // Update the NetworkVariable! All clients will instantly see the color change.
        syncedCurrentColor.Value = (CardColor)colorIndex;
        Debug.Log($"Host verified Color changed to: {syncedCurrentColor.Value}");

        if (pendingWildCard != null)
        {
            ApplyBasicCardEffect(pendingWildCard);
            pendingWildCard = null;
            UpdateTurnVisuals();
        }
        else if (lastPlus4Player != null)
        {
            lastPlus4Player = null;
            UpdateTurnVisuals();
        }
    }

    private void ApplyBasicCardEffect(CardScriptables card)
    {
        switch (card.cardValue)
        {
            case CardValue.REVERSE:
                turnManager.ReverseDirection();
                MoveToNextActivePlayer();
                break;

            case CardValue.SKIP:
                //turnManager.NextTurn(extraSkip: 1);
                MoveToNextActivePlayer(extraSkip: 1);
                break;

            // --- CẬP NHẬT LUẬT STACKING (4.5) ---
            case CardValue.PLUS2:
                currentPenalty += 2;
                MoveToNextActivePlayer(); 
                break;

            case CardValue.PLUS4:
                currentPenalty += 4;
                MoveToNextActivePlayer();
                break;

            // --- CUSTOM RULES HOOKS (4.1, 4.2, 4.3) ---
            case CardValue.ZERO:
                Debug.Log("Rule of 0: Trigger UI to choose Direction.");
                // TODO: Dừng turn logic ở đây, hiển thị UI chọn chiều. 
                // Sau khi user chọn, gọi hàm ExecuteRule0(int direction)
                panelRule0.SetActive(true);
                break;

            case CardValue.SEVEN:
                Debug.Log("Rule of 7: Trigger UI to choose Target Player.");
                // TODO: Dừng turn, hiển thị UI chọn mục tiêu.
                // Sau khi chọn, gọi hàm ExecuteRule7(PlayerController target)
                //panelRule7.SetActive(true);
                ShowRule7UI(turnManager.CurrentPlayerIndex);
                break;

            case CardValue.EIGHT:
                Debug.Log("Rule of 8: Trigger Reaction Event!");
                StartCoroutine(Rule8ReactionEventRoutine());
                break;

            default:
                MoveToNextActivePlayer();
                break;
        }
    }


    private void UpdateTurnVisuals()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetTurnState(i == turnManager.CurrentPlayerIndex);
        }

        Debug.Log($"Current turn: Player {turnManager.CurrentPlayerIndex}, direction: {turnManager.Direction}");
    }
    //nhi: chinh cach nut Draw hoat dong
    /*Người chơi chỉ được Draw khi:
        1. Đang tới lượt mình
        2. Không có lá bài legal nào trong hand

        Sau khi Draw:
        - Nếu lá vừa rút playable → vẫn giữ lượt, người chơi có thể đánh lá đó
        - Nếu lá vừa rút không playable → kết thúc lượt, chuyển sang player tiếp theo
    */
    public void RequestCurrentPlayerDraw()
    {
        if (turnManager == null || players == null || players.Count == 0)
        {
            Debug.LogWarning("Cannot draw because TurnManager or players list is not ready.");
            return;
        }

        PlayerController currentPlayer = players[turnManager.CurrentPlayerIndex];

        TryDrawCard(currentPlayer);
    }

    public void TryDrawCard(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot draw because player is null.");
            return;
        }

        if (turnManager == null)
        {
            Debug.LogWarning("Cannot draw because TurnManager is not ready.");
            return;
        }

        if (player.PlayerIndex != turnManager.CurrentPlayerIndex)
        {
            Debug.Log($"Player {player.PlayerIndex}: Not your turn, cannot draw.");
            return;
        }

        if (player.Hand.HasAnyLegalCard(player))
        {
            Debug.Log($"Player {player.PlayerIndex}: You still have legal cards, cannot draw.");
            return;
        }

        CardScriptables drawnCard = player.DrawCard();

        bool drawnCardPlayable = IsLegalMove(player, drawnCard);

        if (drawnCardPlayable)
        {
            Debug.Log($"Player {player.PlayerIndex} drew a playable card: {drawnCard.CardName()}. Player can still play.");

            UpdateTurnVisuals();
            return;
        }

        Debug.Log($"Player {player.PlayerIndex} drew an unplayable card. Turn ends.");

        MoveToNextActivePlayer();
        UpdateTurnVisuals();

        if (currentPenalty > 0)
        {
            AcceptDrawPenalty(player);
            return;
        }

        // Logic Draw 1 lá bình thường của bạn...
        CardScriptables _drawnCard = player.DrawCard();
        bool _drawnCardPlayable = IsLegalMove(player, _drawnCard);

        if (_drawnCardPlayable)
        {
            Debug.Log($"Player {player.PlayerIndex} can countinue.");
            UpdateTurnVisuals();
            return;
        }

        MoveToNextActivePlayer();
        UpdateTurnVisuals();
    }

    private void AcceptDrawPenalty(PlayerController player)
    {
        Debug.Log($"Player {player.PlayerIndex} got {currentPenalty} cards and lost turn.");
        for (int i = 0; i < currentPenalty; i++)
        {
            player.DrawCard();
        }
        currentPenalty = 0; // Reset penalty
        // NẾU VỪA BỊ PHẠT BỞI CHUỖI +4
        if (lastPlus4Player != null)
        {
            Debug.Log($"Chain +4 ended. Please, player {lastPlus4Player.PlayerIndex} choose the color!");
            // Bật panel chọn màu. Mặc dù lượt của penalizedPlayer đã hết, 
            // game sẽ tạm dừng ở đây chờ người trước đó chọn màu xong mới chuyển lượt.
            panelColorPicker.SetActive(true);
        }
        else
        {
            // NẾU CHỈ BỊ PHẠT BỞI CHUỖI +2 THƯỜNG
            //MoveToNextActivePlayer(); // Chuyển sang người tiếp theo
            UpdateTurnVisuals();
        }
    }

    // --- COROUTINE LUẬT 8 (Reaction Event) ---
    private IEnumerator Rule8ReactionEventRoutine()
    {
        isReactionEventActive = true;
        rule8Responders.Clear();

        Debug.Log("Rule 8: Bắt đầu đếm ngược 3 giây...");
        // TODO: Viết code bật Panel chứa nút Reaction lên tại đây
        panelRule8.SetActive(true);

        yield return new WaitForSeconds(3f);

        isReactionEventActive = false;
        // TODO: Viết code ẩn Panel Reaction đi tại đây
        panelRule8.SetActive(false);

        // Lọc ra những người không bấm kịp
        List<int> slowPlayers = new List<int>();
        for (int i = 0; i < players.Count; i++)
        {
            if (!rule8Responders.Contains(i))
            {
                slowPlayers.Add(i);
            }
        }

        if (slowPlayers.Count > 0)
        {
            // Phạt TẤT CẢ những ai không bấm
            foreach (int pIndex in slowPlayers)
            {
                Debug.Log($"Player {pIndex} does not react ontime, 2 cards penalty.");
                players[pIndex].DrawCard();
                players[pIndex].DrawCard();
            }
        }
        else
        {
            // Nếu ai cũng bấm, phạt người bấm cuối cùng (nằm ở cuối list)
            int lastPlayerIndex = rule8Responders[rule8Responders.Count - 1];
            Debug.Log($"Player {lastPlayerIndex} have the latest reaction, 2 cards penalty.");
            players[lastPlayerIndex].DrawCard();
            players[lastPlayerIndex].DrawCard();
        }

        Debug.Log("End of Reaction Event.");
        MoveToNextActivePlayer();
        UpdateTurnVisuals();
    }

    // Truyền 1 nếu chọn Clockwise, -1 nếu chọn Counter-Clockwise
    public void ExecuteRule0(int direction)
    {
        panelRule0.SetActive(false);
        // 1. Lưu lại bản sao bài của tất cả người chơi
        List<List<CardScriptables>> allHandsSnapshot = new List<List<CardScriptables>>();
        for (int i = 0; i < players.Count; i++)
        {
            allHandsSnapshot.Add(new List<CardScriptables>(players[i].Hand.Cards));
        }

        // 2. Xóa và gán lại bài theo chiều đã chọn
        for (int i = 0; i < players.Count; i++)
        {
            // Tính toán index của người sẽ nhận bài từ người thứ i
            int targetPlayerIndex = (i + direction + players.Count) % players.Count;

            players[targetPlayerIndex].Hand.ClearHand();

            foreach (CardScriptables card in allHandsSnapshot[i])
            {
                players[targetPlayerIndex].Hand.AddCard(card);
            }
        }

        Debug.Log($"Rule of 0 Executed. Direction: {(direction == 1 ? "Clockwise" : "Counter-Clockwise")}");

        // Kết thúc lượt
        MoveToNextActivePlayer();
        UpdateTurnVisuals();
    }

    // --- HÀM MỚI: TỰ ĐỘNG SINH NÚT CHO LUẬT 7 ---
    private void ShowRule7UI(int currentPlayerIndex)
    {
        panelRule7.SetActive(true);

        // 1. Xóa các nút cũ của lượt trước (nếu có)
        foreach (Transform child in rule7ButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Tạo nút mới dựa trên danh sách người chơi hiện tại
        for (int i = 0; i < players.Count; i++)
        {
            // Bỏ qua người đang đánh bài (không thể tự đổi bài cho mình)
            if (i == currentPlayerIndex) continue;

            // LƯU Ý QUAN TRỌNG: Phải gán giá trị i ra một biến cục bộ để dùng trong AddListener
            int targetIndex = i;

            // Sinh ra nút từ Prefab
            GameObject btnObj = Instantiate(rule7ButtonPrefab, rule7ButtonContainer);

            // Tìm component Text (hoặc TextMeshPro) trên nút để đổi tên
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = $"Switch To Player {targetIndex}";
            }

            // Tìm component Button và tự động gắn sự kiện OnClick
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                // Khi bấm nút, nó sẽ tự động gọi hàm ExecuteRule7 với đúng targetIndex
                btn.onClick.AddListener(() => ExecuteRule7(targetIndex));
            }
        }
    }

    public void ExecuteRule7(int targetPlayerIndex)
    {
        panelRule7.SetActive(false);
        PlayerController currentPlayer = players[turnManager.CurrentPlayerIndex];
        PlayerController targetPlayer = players[targetPlayerIndex];

        // 1. Lưu tạm bài của người đang đánh
        List<CardScriptables> tempCurrentHand = new List<CardScriptables>(currentPlayer.Hand.Cards);

        // 2. Chuyển bài của Target sang Current
        currentPlayer.Hand.ClearHand();
        foreach (CardScriptables card in targetPlayer.Hand.Cards)
        {
            currentPlayer.Hand.AddCard(card);
        }

        // 3. Chuyển bài lưu tạm (của Current) sang Target
        targetPlayer.Hand.ClearHand();
        foreach (CardScriptables card in tempCurrentHand)
        {
            targetPlayer.Hand.AddCard(card);
        }

        Debug.Log($"Rule of 7 Executed. Swapped hands between Player {currentPlayer.PlayerIndex} and Player {targetPlayer.PlayerIndex}");

        // Kết thúc lượt
        MoveToNextActivePlayer();
        UpdateTurnVisuals();
    }

    // Gắn hàm này vào nút "Reaction" trên UI. Khi bấm, truyền PlayerIndex của người bấm vào.
    public void OnPlayerReactedRule8(int playerIndex)
    {
        if (isReactionEventActive && !rule8Responders.Contains(playerIndex))
        {
            rule8Responders.Add(playerIndex);
            Debug.Log($"Player {playerIndex} reacted!");
        }
    }



    ////////////////////////////////////
    ///////////////AddOn////////////////
    ////////////////////////////////////


    // <-- THÊM TOÀN BỘ HÀM NÀY XUỐNG CUỐI SCRIPT -->
    private void UpdateTopDiscardDisplay()
    {
        if (topDiscardCardDisplay != null && topDiscardCard != null)
        {
            topDiscardCardDisplay.sprite = topDiscardCard.cardSprite;
        }
        else
        {
            Debug.LogWarning("Missing Top Discard Card Display or Top Card is null!");
        }
    }

    // Hàm mới thay thế cho việc gọi trực tiếp turnManager.NextTurn()
    private void MoveToNextActivePlayer(int extraSkip = 0)
    {
        int validStepsNeeded = 1 + extraSkip;
        int validStepsTaken = 0;

        while (validStepsTaken < validStepsNeeded)
        {
            // Tiến lên đúng 1 vị trí theo đúng logic cũ của TurnManager
            turnManager.NextTurn(0);

            // Kiểm tra xem người ở vị trí mới này còn bài trên tay hay không
            if (players[turnManager.CurrentPlayerIndex].Hand.Cards.Count > 0)
            {
                validStepsTaken++; // Nếu còn bài thì mới tính là 1 bước hợp lệ
            }
        }
    }

    // Used by the local UI
    public void OpenColorPickerUI()
    {
        panelColorPicker.SetActive(true);
    }

    // Link your new UI Button to this function!
    public void OnHostClickStartMatch()
    {
        if (!IsServer) return; // Only the host can start the game

        // 1. Dynamically find all players that have connected and spawned
        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        players = foundPlayers.OrderBy(p => p.OwnerClientId).ToList();

        // 2. Initialize their indexes
        for (int i = 0; i < players.Count; i++)
        {
            players[i].Init(i);
        }

        turnManager = new TurnManager(players.Count);

        // 3. Deal the cards!
        DealInitialCards();
        InitTopDiscardCardIfNeeded();
        UpdateTurnVisuals();
    }
}