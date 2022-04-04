using System;
using UnityEngine;
 
public class UEH : MonoBehaviour {
    private void Start() {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            Debug.Log($"Unhandled Exception {sender} {args}");
        };
    }
} 