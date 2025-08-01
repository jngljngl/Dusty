using UnityEngine;

public class GizmoVector : MonoBehaviour
{
   

    [SerializeField] public Transform alvo;
    public float campoVisaoGraus = 60f;

    private void OnDrawGizmos()
    {
        if (alvo == null) return;

        // Vetor do objeto para o alvo
        Vector3 direcaoParaAlvo = (alvo.position - transform.position).normalized;

        // Vetor para frente do objeto
        Vector3 frente = transform.forward;

        // Produto escalar
        float alinhamento = Vector3.Dot(frente, direcaoParaAlvo);

        // Visualização dos vetores
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + frente * 2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + direcaoParaAlvo * 2f);

        // Campo de visão
        float anguloLimite = Mathf.Cos(campoVisaoGraus * Mathf.Deg2Rad);
        if (alinhamento > anguloLimite)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.gray;

        Gizmos.DrawWireSphere(alvo.position, 0.3f);

        // Texto de depuração (requer editor avançado, pode usar Debug.Log)
       // Debug.Log($"Alinhamento: {alinhamento:F2}");
    }
}


