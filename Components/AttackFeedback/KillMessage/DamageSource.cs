using System.Numerics;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class DamageSource : MonoBehaviour {
    public string damageType;
    public int damage;
    public TMP_Text message;
    
    private float timer;
    private GameObject placeholder;

    private void Start() {
        timer = 0;
        message = GetComponent<TMP_Text>();
    }

    private void Update() {
        // 200 canvas units a second: positions here are screen pixels, so the canvas scale keeps the
        // pace the same at any HUD size.
        this.transform.position = Vector3.MoveTowards(this.transform.position, placeholder.transform.position, 200f * CanvasScale() * Time.deltaTime);
        
        timer += Time.deltaTime;
        if (timer > 5f) {
            Destroy(placeholder);
            Destroy(gameObject);
        }
    }

    private float CanvasScale() {
        var parent = this.transform.parent;
        return parent != null ? Mathf.Abs(parent.lossyScale.y) : 1f;
    }

    public void InitMessage(string type, int damage, GameObject placeholder) {
        this.transform.localPosition = new Vector3(0, 0, 0);
        this.transform.position = placeholder.transform.position;
        this.damageType = type;
        this.damage = damage;
        this.message.text = damageType + " " + damage;
        this.placeholder = placeholder;
        Reset();
    }
    
    public void Reset() {
        timer = 0;
        message.text = damageType + " " + damage;
    }
}