using UnityEngine;

public class FloatingInteractable : MonoBehaviour
{
   
    [Header("Floating Parameters")]
    [SerializeField] private float floatSpeed = 2f;      // Velocidade da oscila��o
    [SerializeField] private float floatHeight = 0.2f;   // Altura do movimento vertical
    [SerializeField] private bool animate = true;        // Ativar/desativar flutua��o

    private Vector3 startPos;

    void Start()
    {
        // Armazena a posi��o inicial do objeto
        startPos = transform.localPosition;
    }

    void Update()
    {
        if (!animate) return;

        // Cria um offset vertical senoidal com base no tempo
        float offset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Aplica o offset vertical sem alterar X e Z
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }

    // Permite ativar/desativar o efeito externamente
    public void SetFloating(bool isActive)
    {
        animate = isActive;

        // Ao desativar, retorna � posi��o original
        if (!isActive)
            transform.localPosition = startPos;
    }
}
