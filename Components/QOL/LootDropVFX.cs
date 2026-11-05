using System;
using System.Collections;
using BattleImprove;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LootDropVFX : MonoBehaviour{
    public ParticleSystem[] systems;
    private bool hidden;

    private void Start() {
        this.transform.localPosition = new Vector3(0, 0.1f, 0);
        this.transform.localScale = new Vector3(1f, 1f, 1f);
        
        foreach (var system in systems) {
            system.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        }
    }

    private void Update() {
        bool isPressed = BattleImprove.Utils.InputCompat.GetMouseButton(1);
        if (isPressed == hidden) return;

        hidden = isPressed;
        foreach (var system in systems) {
            system.gameObject.SetActive(!isPressed);
        }
    }
}