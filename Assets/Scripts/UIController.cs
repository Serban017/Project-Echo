using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIController : MonoBehaviour
{

    public static UIController instance;

    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    private void Awake()
    {
        instance = this;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
