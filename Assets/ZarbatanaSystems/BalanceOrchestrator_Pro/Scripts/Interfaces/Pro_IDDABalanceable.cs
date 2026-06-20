using System.Collections.Generic;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Interfaces
{
    /// <summary>
    /// Interface for GameObjects/components that want to be balanced dynamically by Google Sheets.
    /// </summary>
    public interface Pro_IDDABalanceable
    {
        /// <summary>
        /// Returns the unique key corresponding to the row in Google Sheets.
        /// </summary>
        string GetDDAKey();

        /// <summary>
        /// Applies the data update from the sheet row.
        /// </summary>
        void ApplyDDAUpdate(Dictionary<string, string> rowData);
    }
}
