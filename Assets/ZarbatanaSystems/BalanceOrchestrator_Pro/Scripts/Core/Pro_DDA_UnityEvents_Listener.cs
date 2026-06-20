using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Interfaces;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Core
{
    [AddComponentMenu("BalanceOrchestrator/Pro Unity Events Listener")]
    public class Pro_DDA_UnityEvents_Listener : MonoBehaviour, Pro_IDDABalanceable
    {
        public enum ExpectedType
        {
            String,
            Int,
            Float,
            Bool
        }

        [Header("DDA Settings")]
        [Tooltip("The unique Key of the row (e.g. 'Level_1' or 'Enemy_Orc').")]
        [SerializeField] private string rowKey;

        [Tooltip("The column header name in Google Sheets (e.g. 'MoveSpeed').")]
        [SerializeField] private string columnName;

        [Tooltip("How should the column value be parsed?")]
        [SerializeField] private ExpectedType expectedType = ExpectedType.String;

        [Header("Events")]
        [SerializeField] private UnityEvent<string> onStringDataReceived;
        [SerializeField] private UnityEvent<int> onIntDataReceived;
        [SerializeField] private UnityEvent<float> onFloatDataReceived;
        [SerializeField] private UnityEvent<bool> onBoolDataReceived;

        private void Start()
        {
            if (Pro_DDA_Manager.Instance != null)
            {
                Pro_DDA_Manager.Instance.ApplyTo(this);
            }
        }

        public string GetDDAKey() => rowKey;

        public void ApplyDDAUpdate(Dictionary<string, string> rowData)
        {
            if (rowData == null) return;

            switch (expectedType)
            {
                case ExpectedType.String:
                    string strVal = rowData.GetString(columnName);
                    onStringDataReceived?.Invoke(strVal);
                    break;

                case ExpectedType.Int:
                    int intVal = rowData.GetInt(columnName);
                    onIntDataReceived?.Invoke(intVal);
                    break;

                case ExpectedType.Float:
                    float floatVal = rowData.GetFloat(columnName);
                    onFloatDataReceived?.Invoke(floatVal);
                    break;

                case ExpectedType.Bool:
                    bool boolVal = rowData.GetBool(columnName);
                    onBoolDataReceived?.Invoke(boolVal);
                    break;
            }
        }
    }
}
