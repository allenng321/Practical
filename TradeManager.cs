using System.Collections.Generic;
using System.Linq;
using Core.Characters;
using Core.Inventory;
using Core.Inventory.Items;
using Core.Trade.NPC;
using Player.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Core.Managers
{
    /// <summary>
    /// This class handles all trade interactions between the player and NPC
    /// Relies on NPCRole.cs
    /// </summary>
    /// <seealso cref="NPCRole"/>
    public class TradeManager : MonoBehaviour
    {
        #region Private Variables 
        
        /// <summary>
        /// Reference that holds all the elements for Trade Window UI
        /// </summary>
        [SerializeField] private GameObject tradeWindowUI;

        /// <summary>
        /// Reference to the background
        /// </summary>
        [SerializeField] private GameObject background;
        
        /// <summary>
        /// Nax number for item cells when trading
        /// Default Value is 28
        /// </summary>
        /// <seealso cref="_itemCellListPlayer"/>>
        /// <seealso cref="_itemCellListNpc"/>>
        [SerializeField] private int maxCellSize = 28;
        
        /// <summary>
        /// Reference to the npc inventory and the role
        /// </summary>
        [SerializeField] private NPCRole npcRole;
        
        /// <summary>
        /// Reference to player info
        /// </summary>
        [SerializeField] private PlayerStats playerStats;
        
        /// <summary>
        /// Reference to npc character
        /// </summary>
        [SerializeField] private BaseCharacter npcChar;
        
        /// <summary>
        /// Reference to the player inventory object
        /// </summary>
        [SerializeField] private InventoryObject playerInv;

        /// <summary>
        /// A NPC Dictionary containing the items and the prices, this is calculated using Method CalculatePrices
        /// </summary>
        /// <seealso cref="CalculatePrices"/>>
        private Dictionary<ItemObject, float> _npcPrices;
        
        /// <summary>
        /// A Player Dictionary containing the items and the prices, this is calculated using Method CalculatePrices
        /// </summary>
        /// <seealso cref="CalculatePrices"/>>
        private Dictionary<ItemObject, float> _playerPrices;

        /// <summary>
        /// A List of GameObjects that holds all item cell object
        /// </summary>
        /// <seealso cref="GetItemCellsObject"/>>
        private List<GameObject> _itemCellListNpc;
        
        /// <summary>
        /// A List of GameObjects that holds all item cell object
        /// </summary>
        /// <seealso cref="GetItemCellsObject"/>>
        private List<GameObject> _itemCellListPlayer;
        
        /// <summary>
        /// Determine whether the NPC that is ready to trade is equals to the last NPC that the player had traded with
        /// </summary>
        /// <seealso cref="npcChar"/>
        private BaseCharacter _lastNpcTradeInteraction;

        /// <summary>
        /// A ItemObject that is changed when the player selects a specific ItemObject
        /// This is frequently used and changed in Method OnClickItemCell
        /// </summary>
        /// <seealso cref="OnClickItemCell"/>
        private ItemObject _selectedItemObject;
        
        /// <summary>
        /// A Image Component from selectedItemObject
        /// </summary>
        /// <seealso cref="_selectedItemObject"/>
        private Image _onSelectedImageCom;
        
        /// <summary>
        /// A bool to check if the item is from NPC
        /// </summary>
        private bool _isItemFromNpc;
         
        /// <summary>
        /// A bool to check if the item is from Player
        /// </summary>
        private bool _isItemFromPlayer;
        
        /// <summary>
        /// A float that is calculated for multiplying the base price of a ItemObject. Example npcRate = 1.5f 
        /// </summary>
        /// <seealso cref="CalculatePrices"/>
        private float _npcRate;
        
        /// <summary>
        /// A float that is calculated for multiplying the base price of a ItemObject. Example playerRate = 1.5f 
        /// </summary>
        /// <seealso cref="CalculatePrices"/>
        private float _playerRate;

        /// <summary>
        /// A GameObject that is changed when the player selects a specific Item Cell
        /// This is changed using Method OnClickItemCell
        /// </summary>
        /// <seealso cref="OnClickItemCell"/>
        private GameObject _selectedItemCell;
        
        /// <summary>
        /// A bool that determine if the player has selected a item cell
        /// </summary>
        private bool _isSelectedItemCell;

        /// <summary>
        ///  A bool is determine if the player has started a trade
        /// </summary>
        private bool _isStartedTrade;
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// This is for initializing every necessary variables and aspects for trading
        /// </summary>
        public void StartTrade(NPCRole npcRoleLocal, PlayerStats playerCharLocal, BaseCharacter npcCharLocal, InventoryObject playerInvLocal)
        {
            if (!npcRole.canTrade || _isStartedTrade) return;
            OpenTradeWindow();
            HideCursor.Show();
            Time.timeScale = 0f;

            _isStartedTrade = true;

            npcRole = npcRoleLocal;
            playerStats = playerCharLocal;
            npcChar = npcCharLocal;
            playerInv = playerInvLocal;
                
            // Limits size of the list
            _itemCellListNpc = new List<GameObject>(maxCellSize);
            _itemCellListPlayer = new List<GameObject>(maxCellSize);
                
            // Get a list of item cell objects
            _itemCellListNpc = GetItemCellsObject(tradeWindowUI.transform.GetChild(1).GetChild(0).GetChild(0).gameObject);
            _itemCellListPlayer = GetItemCellsObject(tradeWindowUI.transform.GetChild(1).GetChild(1).GetChild(0).gameObject);

                
            var cellIndexNpc = 1;
            foreach (var item in _itemCellListNpc)
            {
                try
                {
                    var aInt = ExtractNumberFromString(item.name);
                }
                catch
                {
                    item.name = item.name + " " + cellIndexNpc;
                    cellIndexNpc++;
                }
                    
                var trigger = item.GetComponent<EventTrigger>();
                var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
                entry.callback.AddListener((data) => {OnClickItemCell(item);});

                var newTriggers = new List<EventTrigger.Entry> {entry};

                trigger.triggers = newTriggers;
            }

            var cellIndexPlayer = 1;
            foreach (var item in _itemCellListPlayer)
            {
                try
                {
                    var aInt = ExtractNumberFromString(item.name);
                }
                catch
                {
                    item.name = item.name + " " + cellIndexPlayer;
                    cellIndexPlayer++;
                }
                    
                var trigger = item.GetComponent<EventTrigger>();
                var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
                entry.callback.AddListener((data) => {OnClickItemCell(item);});

                var newTriggers = new List<EventTrigger.Entry> {entry};

                trigger.triggers = newTriggers;
            }

            if (_lastNpcTradeInteraction != npcChar || _lastNpcTradeInteraction == null)
            {
                _npcPrices = CalculatePrices(npcRole.inventory, npcRole.buyRate, true);
                _playerPrices = CalculatePrices(playerInv, npcRole.sellRate, false);
            }
                
            // Set Last Actions
            _lastNpcTradeInteraction = npcChar;

            // Update Wealth string
            UpdateNpcPlayerWealthString();

            // show items in each slots in npc
            UpdateItemSlots(_npcPrices, true);
                
            // show items in each slots in player
            UpdateItemSlots(_playerPrices, false);

            UpdateAmountString();
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Buys item from NPC inventory
        /// The way it works is exchange the money and remove ItemObject from NPC inventory, then adds the ItemObject to Player inventory
        /// </summary>
        private void BuyItem(BaseCharacter playerChar, BaseCharacter npcChar)
        {
            if (_isItemFromNpc)
            {
                var itemPrice = _npcPrices[_selectedItemObject];

                if (playerChar.Gold >= itemPrice)
                {
                    playerChar.Gold -= short.Parse(itemPrice.ToString());
                    npcChar.Gold += short.Parse(itemPrice.ToString());

                    UpdateNpcPlayerWealthString();

                    if (_playerPrices.Count >= 27) return;
                    // Check if player has the item in their inventory to prevent same key error
                    if (!_playerPrices.ContainsKey(_selectedItemObject))
                    {

                        // Add item to player item cell
                        var itemCellToAdd = FindAvailableItemCell(false);

                        var itemCellRawImgToAdd = itemCellToAdd.transform.GetChild(0).GetComponent<Image>();
                    
                        // Set texture
                        itemCellRawImgToAdd.sprite = _selectedItemObject.uiDisplay;
                        var color = itemCellRawImgToAdd.color;
                        color = new Color(color.r, color.g, color.b, 255f);
                        itemCellRawImgToAdd.color = color;

                        _playerPrices.Add(_selectedItemObject, Mathf.RoundToInt(_selectedItemObject.data.price * _playerRate));
                            
                        // Add item to inventory object
                        var emptyInventorySlot = playerInv.FindAvailableSlot(playerInv);
                            
                        emptyInventorySlot.item.Name = _selectedItemObject.name;
                        emptyInventorySlot.item.level = _selectedItemObject.data.level;
                        emptyInventorySlot.item.Id = _selectedItemObject.data.Id;

                        emptyInventorySlot.item.price = _playerPrices[_selectedItemObject];
                        emptyInventorySlot.amount = 1;

                        if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                        if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                        // updating amount string
                        UpdateAmountString();

                    }
                    else if (_selectedItemObject.stackable) // Check to see if the itemObject is stackable
                    {
                        playerInv.FindItemOnInventorySlot(_selectedItemObject).AddAmount(1);
                            
                        if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                        if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                        // updating amount string
                        UpdateAmountString();
                    }
                    else if (_selectedItemObject.stackable == false)
                    {
                        // Add item to player item cell
                        var itemCellToAdd = FindAvailableItemCell(false);
                        
                        var itemCellRawImgToAdd = itemCellToAdd.transform.GetChild(0).GetComponent<Image>();
                        
                        // Set texture
                        itemCellRawImgToAdd.sprite = _selectedItemObject.uiDisplay;
                        var color = itemCellRawImgToAdd.color;
                        color = new Color(color.r, color.g, color.b, 255f);
                        itemCellRawImgToAdd.color = color;

                        // Add item to inventory object
                        var emptyInventorySlot = playerInv.FindAvailableSlot(playerInv);
                            
                        emptyInventorySlot.item.Name = _selectedItemObject.name;
                        emptyInventorySlot.item.level = _selectedItemObject.data.level;
                        emptyInventorySlot.item.Id = _selectedItemObject.data.Id;
                            
                        emptyInventorySlot.item.price = _playerPrices[_selectedItemObject];
                        emptyInventorySlot.amount = 1;
                            
                        // Add Item to dict
                        var itemObject = ScriptableObject.CreateInstance<ItemObject>();
                        itemObject.description = _selectedItemObject.description;
                        itemObject.stackable = _selectedItemObject.stackable;
                        itemObject.type = _selectedItemObject.type;
                        itemObject.characterDisplay = _selectedItemObject.characterDisplay;
                        itemObject.uiDisplay = _selectedItemObject.uiDisplay;
                        itemObject.is1HWeapon = _selectedItemObject.is1HWeapon;
                        itemObject.data.level = _selectedItemObject.data.level;
                        itemObject.data.price = _selectedItemObject.data.price;
                        itemObject.data.Id = _selectedItemObject.data.Id;
                        itemObject.data.Name = _selectedItemObject.data.Name + "123";
                        itemObject.name = _selectedItemObject.name;
                        _playerPrices.Add(itemObject, Mathf.RoundToInt(_playerRate * _selectedItemObject.data.price));
                            
                        if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                        if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                        // Updating amount string
                        UpdateAmountString();
                    }
                        

                    var itemInventoryToEdit = npcRole.inventory.FindItemOnInventorySlot(_selectedItemObject);

                    npcRole.inventory.FindItemOnInventorySlot(_selectedItemObject).RemoveItem(1);

                    // Check the item amount of that inventory slot
                    if (itemInventoryToEdit.amount <= 0)
                    {
                        // Removing all texture from item slots
                        foreach (var selectedItemCellRawImg in _itemCellListNpc.Select(itemSlot => itemSlot.transform.GetChild(0).GetComponent<Image>()))
                        {
                            selectedItemCellRawImg.sprite = null;
                            var color = selectedItemCellRawImg.color;
                            color = new Color(color.r, color.g, color.b, 0f);
                            selectedItemCellRawImg.color = color;
                        }

                        // Set the inventory to null as there will be 0 amount for the specified item
                        itemInventoryToEdit.amount = 0;
                        itemInventoryToEdit.item.Name = null;
                        itemInventoryToEdit.item.Id = -1;

                        _npcPrices.Remove(_selectedItemObject); // Remove the key from dict since there is 0 amount of that item
                            
                        var itemCellToNull = ExtractNumberFromString(_selectedItemCell.name) - 1;

                        var itemCellToNullRawImgCom = _itemCellListNpc[itemCellToNull].transform.GetChild(0).GetComponent<Image>();
                        itemCellToNullRawImgCom.sprite = null;
                        var color1 = itemCellToNullRawImgCom.color;
                        color1 = new Color(color1.r, color1.g, color1.b, 0f);
                        itemCellToNullRawImgCom.color = color1;

                        if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                        if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}

                        EmptyItemDetails(0);
                        _selectedItemCell = null;
                        _isSelectedItemCell = false;
                        _isItemFromPlayer = false;
                        _isItemFromNpc = false;
                    }
                    else
                    {
                        _onSelectedImageCom.enabled = true;

                        if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                        if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                        UpdateAmountString();

                        if (npcRole.inventory.FindItemOnInventorySlot(_selectedItemObject).amount > 0) return;
                        EmptyItemDetails(0);
                        _selectedItemCell = null;
                        _isSelectedItemCell = false;
                        _isItemFromPlayer = false;
                        _isItemFromNpc = false;
                                
                        var selectedItemCellRawImg = _selectedItemCell != null ? _selectedItemCell.transform.GetChild(0).GetComponent<Image>() : null;

                        if (selectedItemCellRawImg is null) return;
                        selectedItemCellRawImg.sprite = null;
                        
                        var color = selectedItemCellRawImg.color;
                        color = new Color(color.r, color.g, color.b, 0f);
                        selectedItemCellRawImg.color = color;
                    }
                }
                else
                {
                    // TODO a message to tell the player that he dont have enough money
                }
            }
            else
            {
                // TODO possibly a message to tell the player that his inventory is full
            }
        }
        
        /// <summary>
        /// Sells item to NPC inventory
        /// The way it works is exchange the money and remove ItemObject from Player inventory, then adds the ItemObject to NPC inventory
        /// </summary>
        private void SellItem(BaseCharacter playerChar, BaseCharacter npcChar)
        {
            if (_isItemFromPlayer)
            {
                var itemPrice = _playerPrices[_selectedItemObject];

                if (npcChar.Gold >= itemPrice)
                {
                    playerChar.Gold += short.Parse(itemPrice.ToString());
                    npcChar.Gold -= short.Parse(itemPrice.ToString());

                    UpdateNpcPlayerWealthString();

                    if (_npcPrices.Count < 27 ) // Prevent value > 28, limit player to buy when inventory is full
                    {
                        // Check if player has the item in their inventory, Brand new item
                        if (!_npcPrices.ContainsKey(_selectedItemObject))
                        {
                            // Add item to NPC item cell
                            var itemCellToAdd = FindAvailableItemCell(true);

                            var itemCellRawImgToAdd = itemCellToAdd.transform.GetChild(0).GetComponent<Image>();
                    
                            // Set texture
                            itemCellRawImgToAdd.sprite = _selectedItemObject.uiDisplay;
                            var color = itemCellRawImgToAdd.color;
                            color = new Color(color.r, color.g, color.b, 255f);
                            itemCellRawImgToAdd.color = color;

                            _npcPrices.Add(_selectedItemObject, Mathf.RoundToInt(_selectedItemObject.data.price * _npcRate));
                            
                            // Add item to inventory object
                            var emptyInventorySlot = npcRole.inventory.FindAvailableSlot(npcRole.inventory);
                            
                            emptyInventorySlot.item.Name = _selectedItemObject.name;
                            emptyInventorySlot.item.level = _selectedItemObject.data.level;
                            emptyInventorySlot.item.Id = _selectedItemObject.data.Id;

                            emptyInventorySlot.item.price = _npcPrices[_selectedItemObject];
                            emptyInventorySlot.amount = 1;

                            if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                            if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                            UpdateAmountString();

                        }
                        else if (_selectedItemObject.stackable) // Check to see if the itemObject is stackable
                        {
                            npcRole.inventory.FindItemOnInventorySlot(_selectedItemObject).AddAmount(1);

                            if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                            if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                            UpdateAmountString();
                        }
                        else if (_selectedItemObject.stackable == false)
                        {
                            // Add item to NPC item cell
                            var itemCellToAdd = FindAvailableItemCell(true);
                        
                            var itemCellRawImgToAdd = itemCellToAdd.transform.GetChild(0).GetComponent<Image>();
                        
                            // Set texture
                            itemCellRawImgToAdd.sprite = _selectedItemObject.uiDisplay;
                            var color = itemCellRawImgToAdd.color;
                            color = new Color(color.r, color.g, color.b, 255f);
                            itemCellRawImgToAdd.color = color;

                            // Add item to inventory object
                            var emptyInventorySlot = npcRole.inventory.FindAvailableSlot(npcRole.inventory);
                            
                            emptyInventorySlot.item.Name = _selectedItemObject.name;
                            emptyInventorySlot.item.level = _selectedItemObject.data.level;
                            emptyInventorySlot.item.Id = _selectedItemObject.data.Id;
                        
                            emptyInventorySlot.item.price = _npcPrices[_selectedItemObject];
                            emptyInventorySlot.amount = 1;
                            
                            // Add Item to dict
                            var itemObject = ScriptableObject.CreateInstance<ItemObject>();
                            itemObject.description = _selectedItemObject.description;
                            itemObject.stackable = _selectedItemObject.stackable;
                            itemObject.type = _selectedItemObject.type;
                            itemObject.characterDisplay = _selectedItemObject.characterDisplay;
                            itemObject.uiDisplay = _selectedItemObject.uiDisplay;
                            itemObject.is1HWeapon = _selectedItemObject.is1HWeapon;
                            itemObject.data.level = _selectedItemObject.data.level;
                            itemObject.data.price = _selectedItemObject.data.price;
                            itemObject.data.Id = _selectedItemObject.data.Id;
                            itemObject.data.Name = _selectedItemObject.data.Name + "123";
                            itemObject.name = _selectedItemObject.name;
                            _npcPrices.Add(itemObject, Mathf.RoundToInt(_npcRate * _selectedItemObject.data.price));
                            
                            if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                            if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                            UpdateAmountString();
                            
                        }
                        
                        var itemInventoryToEdit = playerInv.FindItemOnInventorySlot(_selectedItemObject);

                        playerInv.FindItemOnInventorySlot(_selectedItemObject).RemoveItem(1);
                        
                        // Check the item amount of that inventory slot
                        if (itemInventoryToEdit.amount <= 0)
                        {
                            // Removing all texture from item slots
                            foreach (var selectedItemCellRawImg in _itemCellListPlayer.Select(itemSlot => itemSlot.transform.GetChild(0).GetComponent<Image>()))
                            {
                                selectedItemCellRawImg.sprite = null;
                                var color = selectedItemCellRawImg.color;
                                color = new Color(color.r, color.g, color.b, 0f);
                                selectedItemCellRawImg.color = color;
                            }

                            // Set the inventory to null as there will be 0 amount for the specified item
                            itemInventoryToEdit.amount = 0;
                            itemInventoryToEdit.item.Name = null;
                            itemInventoryToEdit.item.Id = -1;

                            _playerPrices.Remove(_selectedItemObject ); // Remove the key from dict since there is 0 amount of that item

                            var itemCellToNull = ExtractNumberFromString(_selectedItemCell.name) - 1;
                            
                            var itemCellToNullRawImgCom = _itemCellListPlayer[itemCellToNull].transform.GetChild(0).GetComponent<Image>();
                            itemCellToNullRawImgCom.sprite = null;
                            var color1 = itemCellToNullRawImgCom.color;
                            color1 = new Color(color1.r, color1.g, color1.b, 0f);
                            itemCellToNullRawImgCom.color = color1;

                            if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                            if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}

                            EmptyItemDetails(1);
                            _selectedItemCell = null;
                            _isSelectedItemCell = false;
                            _isItemFromPlayer = false;
                            _isItemFromNpc = false;
                        }
                        else
                        {
                            _onSelectedImageCom.enabled = true;

                            if (_npcPrices != null) {UpdateItemSlots(_npcPrices, true);}
                            if (_playerPrices != null){UpdateItemSlots(_playerPrices, false);}
                            
                            UpdateAmountString();

                            if (playerInv.FindItemOnInventorySlot(_selectedItemObject).amount > 0) return;
                            EmptyItemDetails(1);
                            _selectedItemCell = null;
                            _isSelectedItemCell = false;
                            _isItemFromPlayer = false;
                            _isItemFromNpc = false;

                            if (_selectedItemCell is null) return;
                            var selectedItemCellRawImg = _selectedItemCell.transform.GetChild(0).GetComponent<Image>();
                                
                            selectedItemCellRawImg.sprite = null;
                            var color = selectedItemCellRawImg.color;
                            color = new Color(color.r, color.g, color.b, 0f);
                            selectedItemCellRawImg.color = color;
                        }
                    }
                }
                else
                {
                    // TODO a message to tell the player that he dont have enough money
                }
            }
            else
            {
                // TODO possibly a message to tell the player that his inventory is full
            }
        }

        /// <summary>
        /// Updates the wealth of the NPC string and Player string
        /// </summary>
        private void UpdateNpcPlayerWealthString()
        {
            var npcWealthObject = tradeWindowUI.transform.GetChild(1).GetChild(0).GetChild(1).GetChild(3).gameObject;
            var playerWealthObject = tradeWindowUI.transform.GetChild(1).GetChild(1).GetChild(1).GetChild(3).gameObject;
            
            npcWealthObject.GetComponent<TextMeshProUGUI>().text = npcChar.Gold.ToString();
            playerWealthObject.GetComponent<TextMeshProUGUI>().text = playerStats.Gold.ToString();
        }

        /// <summary>
        /// Updates the amount of every existing item in item cells
        /// </summary>
        private void UpdateAmountString()
        {
            foreach (var cell in _itemCellListPlayer)
            {
                if (cell.transform.GetChild(0).GetComponent<Image>().sprite != null)
                {
                    var value = ExtractNumberFromString(cell.name) - 1;
                    cell.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Mathf.RoundToInt(playerInv.FindItemOnInventorySlot(_playerPrices.ElementAt(value).Key).amount).ToString();
                }
                else
                {
                    cell.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "";
                }
            }
                
            foreach (var cell in _itemCellListNpc)
            {
                if (cell.transform.GetChild(0).GetComponent<Image>().sprite != null)
                {
                    var value = ExtractNumberFromString(cell.name) - 1;
                    cell.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Mathf.RoundToInt(npcRole.inventory.FindItemOnInventorySlot(_npcPrices.ElementAt(value).Key).amount).ToString();
                }
                else
                {
                    cell.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "";
                }
            }
        }
        
        /// <summary>
        /// Get a GameObject that contains all item cell objects
        /// </summary>
        /// <returns>List of GameObjects</returns>>
        private static List<GameObject> GetItemCellsObject(GameObject itemCellObject)
        {
            return (from Transform item in itemCellObject.transform select item.gameObject).ToList();
        }

        /// <summary>
        /// Calculate and returns a dictionary with the ItemObject and the price
        /// </summary>
        /// <seealso cref="_playerPrices"/>
        /// <seealso cref="_npcPrices"/>
        /// <returns>A Dictionary with ItemObject as Key and float as Value</returns>
        private Dictionary<ItemObject, float> CalculatePrices(InventoryObject inventory, (float, float) tupleRate, bool isNPC)
        {

            var itemDict = new Dictionary<ItemObject, float>();
            
            var randomNum = Random.Range(tupleRate.Item1, tupleRate.Item2);

            if (isNPC){_npcRate = randomNum;}
            else{_playerRate = randomNum;}

            var itemList = inventory.GetAllItems();

            foreach (var item in itemList)
            {
                var itemNewPrice = Mathf.RoundToInt(item.data.price * randomNum);

                if (itemDict.ContainsKey(item))
                {
                    var itemObject = ScriptableObject.CreateInstance<ItemObject>();
                    itemObject.description = item.description;
                    itemObject.stackable = item.stackable;
                    itemObject.type = item.type;
                    itemObject.characterDisplay = item.characterDisplay;
                    itemObject.uiDisplay = item.uiDisplay;
                    itemObject.is1HWeapon = item.is1HWeapon;
                    itemObject.data.level = item.data.level;
                    itemObject.data.price = item.data.price;
                    itemObject.data.Id = item.data.Id;
                    itemObject.data.Name = item.data.Name;
                    itemObject.name = item.name;
                    itemDict.Add(itemObject, itemNewPrice);
                    continue;
                }
                
                itemDict.Add(item, itemNewPrice);

                inventory.FindItemOnInventorySlot(item).item.price = itemNewPrice; // set inventory prices
            }

            return itemDict;
        }

        /// <summary>
        /// Updates the texture of every Item Slots if the item cells contains a ItemObject
        /// </summary>
        private void UpdateItemSlots(Dictionary<ItemObject, float> dict, bool isNPC)
        {
            var cellIndex = 0;

            if (isNPC)
            {
                foreach (var itemObject in dict)
                {
                    var itemCell = _itemCellListNpc[cellIndex];
                    var itemCellRawImage = itemCell.transform.GetChild(0).GetComponent<Image>();
                    
                    itemCellRawImage.sprite = itemObject.Key.uiDisplay;
                    var color = itemCellRawImage.color;
                    color = new Color(color.r, color.g, color.b, 255f);
                    itemCellRawImage.color = color;

                    UpdateAmountString();
                    cellIndex++;
                }
            }
            else
            {
                foreach (var itemObject in dict)
                {
                    var itemCell = _itemCellListPlayer[cellIndex];
                    var itemCellRawImage = itemCell.transform.GetChild(0).GetComponent<Image>();
                    
                    itemCellRawImage.sprite = itemObject.Key.uiDisplay;
                    var color = itemCellRawImage.color;
                    color = new Color(color.r, color.g, color.b, 255f);
                    itemCellRawImage.color = color;

                    UpdateAmountString();
                    cellIndex++;
                }
            }
            
        }
        
        /// <summary>
        /// Updates the Item Details for the specified inventory. Example: 0 for NPC, 1 for Player
        /// This shows the price, description, icon, level, and the style of an item
        /// </summary>
        private void SetItemDetails(int index, int itemDictIndex)
        {
            var itemIcon = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(0).gameObject;
            var itemName = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(1).gameObject;
            var itemDescription = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(2).gameObject;
            var coinAmount = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(4).gameObject;
            var xpAmount = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(6).gameObject;
            var style = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(8).gameObject;
            
            var itemIconChild = itemIcon.GetComponentInChildren<Image>();
            itemIconChild.sprite = _selectedItemObject.uiDisplay;
            var color = itemIconChild.color;
            color = new Color(color.r, color.g, color.b, 255f);
            itemIconChild.color = color;

            itemName.GetComponent<TextMeshProUGUI>().text = _selectedItemObject.name;
            itemDescription.GetComponent<TextMeshProUGUI>().text = _selectedItemObject.description;

            coinAmount.GetComponent<TextMeshProUGUI>().text = index == 0 ? _npcPrices.ElementAt(itemDictIndex).Value.ToString() : _playerPrices.ElementAt(itemDictIndex).Value.ToString();
            xpAmount.GetComponent<TextMeshProUGUI>().text = _selectedItemObject.data.level.ToString();
            style.GetComponent<TextMeshProUGUI>().text = _selectedItemObject.type.ToString();
        }

        /// <summary>
        /// Empties the Item Details for the specified inventory. Example: 0 for NPC, 1 for Player
        /// This remove the visuals of the price, description, icon, level, and the style of an item
        /// </summary>
        private void EmptyItemDetails(int index)
        {
            if (_selectedItemCell != null)
            {
                var onSelectedImageCom = _selectedItemCell.transform.GetChild(1).GetComponent<Image>();
                onSelectedImageCom.enabled = false;
            }

            var itemIcon = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(0).gameObject;
            var itemName = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(1).gameObject;
            var itemDescription = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(2).gameObject;
            var coinAmount = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(4).gameObject;
            var xpAmount = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(6).gameObject;
            var style = tradeWindowUI.transform.GetChild(1).GetChild(index).GetChild(2).GetChild(8).gameObject;
            
            var itemIconChild = itemIcon.GetComponentInChildren<Image>();
            itemIconChild.sprite = null;
            var color = itemIconChild.color;
            color = new Color(color.r, color.g, color.b, 0);
            itemIconChild.color = color;

            itemName.GetComponent<TextMeshProUGUI>().text = null;
            itemDescription.GetComponent<TextMeshProUGUI>().text = null;
            coinAmount.GetComponent<TextMeshProUGUI>().text = null;
            xpAmount.GetComponent<TextMeshProUGUI>().text = null;
            style.GetComponent<TextMeshProUGUI>().text = null;
        }

        /// <summary>
        /// A function that find available item cell
        /// </summary>
        /// <returns>GameObject</returns>
        private GameObject FindAvailableItemCell(bool isNPC)
        {
            return isNPC ? _itemCellListNpc.FirstOrDefault(itemCell => itemCell.transform.GetChild(0).GetComponent<Image>().sprite == null) : _itemCellListPlayer.FirstOrDefault(itemCell => itemCell.transform.GetChild(0).GetComponent<Image>().sprite == null);
        }

        /// <summary>
        /// Extracts the numbers from a string. Example: "CoolText123", Output: 123
        /// </summary>
        /// <returns>int</returns>>
        private static int ExtractNumberFromString(string theString)
        {
            var a = theString.Where(char.IsDigit).Aggregate(string.Empty, (current, t) => current + t);

            return short.Parse(a);
        }

        /// <summary>
        /// Enables the trade window ui
        /// </summary>
        /// <seealso cref="tradeWindowUI"/>
        private void OpenTradeWindow()
        {
            tradeWindowUI.SetActive(true);
            background.SetActive(true);

            GetComponent<TradeManager>().enabled = true;
        }

        
        /// <summary>
        /// Closes the trade window ui and set back the time scale to 1
        /// </summary>
        /// <seealso cref="tradeWindowUI"/>
        public void CloseTradeWindow()
        {
            tradeWindowUI.SetActive(false);
            background.SetActive(false);
            
            Time.timeScale = 1f;
            _isStartedTrade = false;
            HideCursor.Hide();

            // selectedItemObject = null;
            // isSelectedItemCell = false;
            // selectedItemCell = null;
            //
            // GetComponent<TradeManager>().enabled = false;
        }

        #endregion


        #region Event Trigger Methods

        /// <summary>
        /// A trigger method that will be called when the player clicks a item cell
        /// This controls the selectedItemObject and selectedItemCell
        /// </summary>
        private void OnClickItemCell(GameObject itemCell)
        {
            var effectiveItemCellCount = 0; 
            
            _onSelectedImageCom = itemCell.transform.GetChild(1).GetComponent<Image>();

            if (!_isSelectedItemCell) {_selectedItemCell = itemCell;}

            // Get ItemObject be in the item cells
            var itemDictIndex = ExtractNumberFromString(_selectedItemCell.name) - 1;

            var parent = itemCell.transform.parent.parent;
            effectiveItemCellCount = parent.name == "NPC" ? _itemCellListNpc.Count(cell => cell.transform.GetChild(0).GetComponent<Image>().sprite != null) : _itemCellListPlayer.Count(cell => cell.transform.GetChild(0).GetComponent<Image>().sprite != null);
            
            switch (parent.name)
            {
                case "NPC" when effectiveItemCellCount >= ExtractNumberFromString(_selectedItemCell.name) && !_isSelectedItemCell:
                    _selectedItemObject = _npcPrices.ElementAt(itemDictIndex).Key;
                    _isSelectedItemCell = true;
                    _isItemFromNpc = true;
                    break;
                case "Player" when effectiveItemCellCount >= ExtractNumberFromString(_selectedItemCell.name) && !_isSelectedItemCell:
                    _selectedItemObject = _playerPrices.ElementAt(itemDictIndex).Key;
                    _isSelectedItemCell = true;
                    _isItemFromPlayer = true;
                    break;
            }

            if (_onSelectedImageCom.enabled == false && _selectedItemCell == itemCell && _isSelectedItemCell)
            {
                // Prevent User clicking null item cells
                if (itemCell.transform.GetChild(0).GetComponent<Image>().sprite == null) {;return;}
                
                _onSelectedImageCom.enabled = true;

                if (_isItemFromNpc)
                {
                    SetItemDetails(0, itemDictIndex);
                }
                else if (_isItemFromPlayer)
                {
                    SetItemDetails(1, itemDictIndex);
                }

            }
            else if (_onSelectedImageCom.enabled && _isSelectedItemCell)
            {
                // Make everything null as player had deselected the item

                _onSelectedImageCom.enabled = false;
                _isSelectedItemCell = false;
                _selectedItemCell = null;

                if (_isItemFromNpc)
                {
                    EmptyItemDetails(0);

                    _isItemFromNpc = false;
                }
                else if (_isItemFromPlayer)
                {
                    EmptyItemDetails(1);

                    _isItemFromPlayer = false;
                }
            }
        }
        
        /// <summary>
        /// A trigger method that will be called when the player pressed the buy button
        /// </summary>
        public void OnPressBuyButton()
        {
            BuyItem(playerStats, npcChar);
        }

        /// <summary>
        /// A trigger method that will be called when the player pressed the sell button
        /// </summary>
        public void OnPressSellButton()
        {
            SellItem(playerStats, npcChar);
        }
        
        /// <summary>
        /// A trigger method that will change the button color
        /// </summary>
        public void OnEnterButton(GameObject buttonObject)
        {
            var buttonObjectRawImgCom = buttonObject.transform.GetChild(2).GetComponent<RawImage>();
            buttonObjectRawImgCom.enabled = !buttonObjectRawImgCom.enabled;
        }

        #endregion

    }
}