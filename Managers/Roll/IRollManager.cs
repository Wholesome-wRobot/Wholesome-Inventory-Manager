﻿namespace Wholesome_Inventory_Manager.Managers.Roll
{
    internal interface IRollManager : ICycleable
    {
        void CheckLootRoll(int rollId);
    }
}