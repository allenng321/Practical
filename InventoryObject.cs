using System.Collections.Generic;
using System.Linq;
using Core.Databases.Inventory;
using Core.Inventory.Items;
using Core.Quest;
using Core.Save;
using UI.Items;
using UnityEditor;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Types of Objects that may contains Items
    /// </summary>
    /// <seealso cref="ItemObject"/>
    public enum InterfaceType
    {
        Inventory,
        Equipment,
        Storage,
        QuestInventory
    }

    /// <summary>
    /// Container that contains Items and methods that handles the main operation with these items inside the container
    /// </summary>
    /// <inheritdoc cref="ScriptableObject"/>
    /// <seealso cref="ItemDatabaseObject"/>
    /// <seealso cref="InterfaceType"/>
    /// <seealso cref="Inventory"/>
    /// <seealso cref="InventorySlot"/>
    [CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/New Inventory")]
    public class InventoryObject : ScriptableObject
    {
        #region Variables

        /// <summary>
        /// Reference to the Database where this InventoryObject belongs to
        /// </summary>
        /// <seealso cref="ItemDatabaseObject"/>
        public ItemDatabaseObject database;
        /// <summary>
        /// Defines the type of this container object
        /// </summary>
        /// <seealso cref="InterfaceType"/>
        public InterfaceType type;
        /// <summary>
        /// Container with all items
        /// </summary>
        /// <seealso cref="Inventory"/>
        /// <seealso cref="InventorySlot"/>
        public Inventory container;
        
        /*public InventorySlot[] GetSlots => Container.Slots;*/
        /// <summary>
        /// Getter for all slots from the <see cref="container"/>
        /// </summary>
        /// <seealso cref="InventorySlot"/>
        public InventorySlot[] GetSlots => container.Slots;

        /// <summary>
        /// List of all items in the container
        /// </summary>
        /// <seealso cref="ItemObject"/>
        private List<ItemObject> itemObjects;

        /// <summary>
        /// Counter of empty slots in the <see cref="InventoryObject"/>
        /// </summary>
        private int EmptySlotCount
        {
            get
            {
                return GetSlots.Count(t => t.item.Id <= -1);
            }
        }        
        
        #endregion

        #region Save & Load Methods

        /// <summary>
        /// Saves this <see cref="InventoryObject"/> into inv folder
        /// </summary>
        [ContextMenu("Save")]
        public void Save()
        {
            SaveManager.Save("inv", name.ToLower() + name.Length + "_inv",this);
        }
        
        /// <summary>
        /// Loads this <see cref="InventoryObject"/> from inv folder
        /// </summary>
        [ContextMenu("Load")]
        public void Load()
        {
            SaveManager.Load("inv", name.ToLower() + name.Length + "_inv", this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the specified item to the inventory with the specified amount
        /// </summary>
        /// <param name="_item">Item that should be added</param>
        /// <param name="_amount">Amount of item that should be added</param>
        /// <returns>If the operation completed successfully</returns>
        public bool AddItem(Item _item, int _amount)
        {
            if (EmptySlotCount <= 0)
                return false;
            var slot = FindItemOnInventory(_item);
            if (!database.ItemObjects[_item.Id].stackable || slot == null)
            {
                SetEmptySlot(_item, _amount);
                Save();
                QuestManager.Instance.CheckItemCollected();
                return true;
            }
            slot.AddAmount(_amount);
            Save();
            QuestManager.Instance.CheckItemCollected();
            return true;
        }

        /// <summary>
        /// Finds the Item on the InventorySlot
        /// </summary>
        /// <param name="itemObject">Item that should be found</param>
        /// <returns>Item that we are looking for</returns>
        public InventorySlot FindItemOnInventorySlot(ItemObject itemObject)
        {
            return GetSlots.FirstOrDefault(t => t.item.Id == itemObject.data.Id);
        }

        #endregion
        
        /// <summary>
        /// Finds the Item on the InventorySlot
        /// </summary>
        /// <param name="item">Item that should be found</param>
        /// <returns>Item that we are looking for</returns>
        private InventorySlot FindItemOnInventory(Item item)
        {
            return GetSlots.FirstOrDefault(t => t.item.Id == item.Id);
        }
        
        /// <summary>
        /// Returns all the items in the inventory.
        /// </summary>
        /// <returns>List of all Items in inventory</returns>
        public List<ItemObject> GetAllItems()
        {
            itemObjects.Clear();
            foreach (var t in GetSlots)
            {
                if (ItemNotInDB(t)) continue;
                itemObjects.Add(database.GetItem(t.item.Id));
            }

            return itemObjects;
        }
        
        /// <summary>
        /// Checks and returns if the specified item is in the inventory regardless of the amount.
        /// </summary>
        /// <param name="itemToFind">Item that we are looking for</param>
        /// <returns>If the item is in the inventory</returns>
        public bool CheckItemInInventory(ItemObject itemToFind)
        {
            /*return GetSlots.Where((t, i) => GetSlots.ElementAt(i).item.Id == itemToFind.data.Id).Any();*/
            return GetSlots.Any(t => t.item.Id == itemToFind.data.Id);
        }

        
        /// <summary>
        /// Checks and returns if the specified item and amount are in the inventory.
        /// </summary>
        /// <param name="itemToFind">Item that we are looking for</param>
        /// <param name="amount">Amount of this item</param>
        /// <returns>If there are enough amount of items we are looking for in the inventory</returns>
        public bool CheckItemInInventory(ItemObject itemToFind, int amount)
        {
            return GetSlots.Any(t => t.item.Id == itemToFind.data.Id && t.amount == amount);
        }

        /// <summary>
        /// Checks and returns the amount of the specified items are in the inventory.  If -1 means there is non of that item in the inventory.
        /// </summary>
        /// <param name="itemToFind">Item we are looking for</param>
        /// <returns>Amount of the item we are looking for</returns>
        public int ItemAmountOfItem(ItemObject itemToFind)
        {
            foreach (var t in GetSlots)
            {
                if (itemToFind.data.Id == t.item.Id)
                {
                    return t.amount;
                }
            }

            return -1;
        }
        
        /// <summary>
        /// Checks and returns if the specified items are in the inventory.
        /// </summary>
        /// <param name="itemsToFind">Items we are looking for</param>
        /// <param name="amount">Their amount</param>
        /// <returns>If there are enough of each items that we are looking for in the inventory</returns>
        public bool CheckItemInInventory(ItemObject[] itemsToFind, int amount)
        {
            return itemsToFind.Select((t, i) => GetSlots.ElementAt(i).item.Id.Equals(t.data.Id) && GetSlots.ElementAt(i).amount.Equals(amount)).FirstOrDefault();
        }

        /// <summary>
        /// Checks and returns if the Item in the inventory is 1Handed or 2Handed Weapon.
        /// </summary>
        /// <param name="item">Item we are checking</param>
        /// <returns>If the Item we were checking is 1Handed or 2Handed</returns>
        public bool IsWeapon1H(ItemObject item)
        {
            return item.is1HWeapon;
        }

        /// <summary>
        /// Empties the specified inventory slot
        /// </summary>
        /// <param name="_item">Item we are looking for</param>
        /// <param name="_amount">Amount of this Item that we will need to reduce its amount by</param>
        /// <returns>Updated slot</returns>
        private InventorySlot SetEmptySlot(Item _item, int _amount)
        {
            foreach (var t in GetSlots)
            {
                if (t.item.Id > -1) continue;
                t.UpdateSlot(_item, _amount);
                return t;
            }

            //set up functionality for full inventory
            //if inventory is full. disable pickups and send Inv full msg
            return null;
        }


        /// <summary>
        /// Checks if the inventory specified has the item specified in it and the correct amount of that item, returns that item
        /// </summary>
        /// <param name="inventory">Reference to the inventory we should look for</param>
        /// <param name="itemToFind">Item we are looking for</param>
        /// <param name="amount">Amount of the item we are looking for</param>
        /// <returns>Item that was requested in parameter or null</returns>
        public ItemObject FindItemInInventory(InventoryObject inventory, ItemObject itemToFind, int amount)
        {
            for (var i = 0; i < inventory.container.Slots.Length; i++)
            {
                if (inventory.container.Slots.ElementAt(i).item.Id.Equals(itemToFind.data.Id) && inventory.container.Slots.ElementAt(i).amount.Equals(amount))
                {
                    return inventory.container.Slots.ElementAt(i).ItemObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Check a specified slot if that slot has an item
        /// </summary>
        /// <returns>Status of item being in the DB</returns>
        private bool ItemNotInDB(InventorySlot inventorySlot)
        {
            if (inventorySlot.item.Id == -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Find an available slot in a specified inventory
        /// </summary>
        /// <returns>Available slot or null</returns>
        public InventorySlot FindAvailableSlot(InventoryObject inventory)
        {
            foreach (var slot in inventory.container.Slots)
            {
                if (ItemNotInDB(slot))
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// Resets base information of every slot in the Inventory
        /// </summary>
        [ContextMenu("Reset Inventory")]
        public void Reset()
        {
            for (var i = 0; i < GetSlots.Length; i++)
            {
                GetSlots.ElementAt(i).item.Id = -1;
                GetSlots.ElementAt(i).item.Name = "";
                GetSlots.ElementAt(i).amount = 0;
                GetSlots.ElementAt(i).item.level = 0;
                GetSlots.ElementAt(i).item.price = 0;

            }
        }

        /// <summary>
        /// Swaps two items in the inventory
        /// </summary>
        /// <param name="item1">First Item to be swapped</param>
        /// <param name="item2">Second Item to be swapped</param>
        /// <seealso cref="InventorySlot"/>
        public void SwapItems(InventorySlot item1, InventorySlot item2)
        {
            if (!item2.CanPlaceInSlot(item1.ItemObject) || !item1.CanPlaceInSlot(item2.ItemObject)) return;
            var temp = new InventorySlot(item2.item, item2.amount);
            item2.UpdateSlot(item1.item, item1.amount);
            item1.UpdateSlot(temp.item, temp.amount);
        }
    }

    /// <summary>
    /// Container that contains a variable with all the slots of <see cref="InventoryObject"/>
    /// </summary>
    /// <seealso cref="InventoryObject"/>
    /// <seealso cref="InventorySlot"/>
    [System.Serializable]
    public class Inventory
    {
        /// <summary>
        /// All items slots of the container
        /// </summary>
        public InventorySlot[] Slots = new InventorySlot[28];
        //public List<InventorySlot> Slots = new List<InventorySlot>();
        
        /// <summary>
        /// Clears the Inventory and removes all the items in the slots
        /// </summary>
        public void Clear()
        {
            foreach (var t in Slots)
            {
                t.RemoveItem();
            }
        }
    }

    public delegate void SlotUpdated(InventorySlot _slot);


    /// <summary>
    /// Simplest unit that contains all the information about one slot of the inventory
    /// </summary>
    /// <seealso cref="InventoryObject"/>
    [System.Serializable]
    public class InventorySlot
    {
        /// <summary>
        /// Array with allowed <see cref="ItemType"/> values that can be stored in this slot
        /// </summary>
        public ItemType[] AllowedItems = new ItemType[0];
        /// <summary>
        /// Parent User Interface reference
        /// </summary>
        [System.NonSerialized]
        public ItemUserInterface parent;
        /// <summary>
        /// Reference to the UI Game Object of this slot
        /// </summary>
        [System.NonSerialized]
        public GameObject slotDisplay;
        /// <summary>
        /// Pointer to the Delegate Method that is called after the slot updates
        /// </summary>
        [System.NonSerialized]
        public SlotUpdated OnAfterUpdate;
        /// <summary>
        /// Pointer to the Delegate Method that is called before the slot updates
        /// </summary>
        [System.NonSerialized]
        public SlotUpdated OnBeforeUpdate;
        /// <summary>
        /// Contains core information about <see cref="ItemObject"/>
        /// </summary>
        /// <seealso cref="Item"/>
        public Item item = new Item();
        /// <summary>
        /// Amount of the <see cref="item"/> in this Inventory Slot
        /// </summary>
        public int amount;

        /// <summary>
        /// Reference to the <see cref="ItemObject"/> that is stored in this slot
        /// </summary>
        public ItemObject ItemObject => item.Id >= 0 ? parent.inventory.database.ItemObjects[item.Id] : null;

        /// <summary>
        /// Default Constructor that updates slot with freshly created Item
        /// </summary>
        public InventorySlot()
        {
            UpdateSlot(new Item(), 0);
        }

        /// <summary>
        /// Constructor that initializes the Inventory slot with the information from parameters
        /// </summary>
        /// <param name="item">Information about the Item that data should be initialized with</param>
        /// <param name="amount">Amount of this Item in this slot</param>
        public InventorySlot(Item item, int amount)
        {
            UpdateSlot(item, amount);
        }

        /// <summary>
        /// Called on every slot update
        /// </summary>
        /// <param name="item">Sets an Item Reference to this slot</param>
        /// <param name="amount">Sets Item Amount in this slot</param>
        public void UpdateSlot(Item item, int amount)
        {
            OnBeforeUpdate?.Invoke(this);
            this.item = item;
            this.amount = amount;

            OnAfterUpdate?.Invoke(this);
        }

        /// <summary>
        /// Removes the item and sets the slot to a blank
        /// </summary>
        public void RemoveItem()
        {
            UpdateSlot(new Item(), 0);
        }

        /// <summary>
        /// Removes a specified amount of the specified item
        /// </summary>
        /// <param name="amount1"></param>
        public void RemoveItem(int value)
        {
            switch (this.amount)
            {
                case 0:
                    return;
                case 1:
                    RemoveItem();
                    break;
                default:
                    UpdateSlot(item, amount -= value);
                    break;
            }
        }

        /// <summary>
        /// Adds more of that item, if the item is stackable then they will stack, if not will be placed in the inventory
        /// </summary>
        /// <param name="value">Value that the amount should be increased by</param>
        public void AddAmount(int value)
        {
            UpdateSlot(item, amount += value);
        }

        /// <summary>
        /// Checks if the item can be placed at that slot
        /// </summary>
        /// <param name="_itemObject">Check if this item can be placed into this slot</param>
        /// <returns></returns>
        public bool CanPlaceInSlot(ItemObject _itemObject)
        {
            if (AllowedItems.Length <= 0 || _itemObject == null || _itemObject.data.Id < 0)
                return true;
            return AllowedItems.Any(t => _itemObject.type == t);
        }
    }
}
