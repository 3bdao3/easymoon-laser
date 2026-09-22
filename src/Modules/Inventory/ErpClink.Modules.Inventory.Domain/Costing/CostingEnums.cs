namespace ErpClink.Modules.Inventory.Domain.Costing;

public enum InventoryCostLayerStatus
{
    Open = 0,
    FullyConsumed = 1,
    Reversed = 2
}

public enum InventoryCostTransactionType
{
    Receipt = 0,
    Issue = 1,
    AdjustmentIn = 2,
    AdjustmentOut = 3,
    Reversal = 4
}

public enum InventoryValuationMethodCode
{
    /// <summary>Provisional default until product approves a method (ADR-015).</summary>
    ProvisionalFifo = 0,
    WeightedAverage = 1
}
