using UnityEngine;
using UnityEngine.SceneManagement; // Importar para gestionar escenas
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public SoundManager soundManager; // Referencia al script SoundManager
    public float moveDistance = 1f; // Tamaño del movimiento
    public LayerMask obstacleLayer; // Capa para paredes y obstáculos
    public LayerMask boxLayer; // Capa para las cajas
    //public Text scoreText; // Referencia al texto de la puntuación en la UI
    public GameObject levelCompleteImagePrefab; // Prefab del objeto PNG para "Nivel Completado"
    public Image fixedImage; // Imagen fija que aparecerá en pantalla
    public Image blinkingImage; // Imagen que parpadeará hasta que el jugador pulse una tecla
    public Image backgroundImage; // Imagen que será el fondo que cubre toda la pantalla
    public float growSpeed = 2f; // Velocidad de crecimiento del PNG
    public Vector2 finalSize = new Vector2(5f, 5f); // Tamaño final del PNG

    private Vector2 targetPosition; // Posición objetivo del jugador
    private int score = 0; // Contador de puntos (cajas colocadas en metas)

    private int totalBoxes; // Número total de cajas en el nivel
    private bool isMoving = false; // Flag para controlar el movimiento
    private bool levelComplete = false; // Flag para indicar si el nivel está completo
    private bool gameStarted = false; // Flag para indicar si el jugador ha comenzado el juego
    private System.Collections.Generic.List<Vector2> blockedPositions = new System.Collections.Generic.List<Vector2>(); // Lista de posiciones bloqueadas

    void Start()
    {
        targetPosition = transform.position; // Inicializa la posición del jugador

        // Contar todas las cajas en el nivel
        totalBoxes = GameObject.FindGameObjectsWithTag("Caja").Length;
        //UpdateScoreUI(); // Actualiza el marcador con la puntuación inicial

        // Inicia la rutina de parpadeo de la imagen
        if (blinkingImage != null)
        {
            StartCoroutine(BlinkStartImage());
        }

        // Asegura que el fondo cubra toda la pantalla
        if (backgroundImage != null)
        {
            backgroundImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height); // Tamaño de fondo pantalla completa
        }
    }

    void Update()
    {
        // Si el jugador no ha iniciado el juego, espera una tecla
        if (!gameStarted)
        {
            if (Input.anyKeyDown) // Detecta si se presiona cualquier tecla
            {
                soundManager.PlaySound(soundManager.playSound); // Reproduce el sonido de Play
                StartGame();
            }
            return;
        }

        // Si el jugador ya está moviéndose o el nivel está completo, no permite otra entrada
        if (isMoving || levelComplete) return;

        // Detectar entrada del jugador
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) // Arriba
            TryMove(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) // Abajo
            TryMove(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) // Izquierda
            TryMove(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) // Derecha
            TryMove(Vector2.right);

        // Reiniciar el nivel al pulsar R
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    void TryMove(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)transform.position + direction * moveDistance;

        // Verifica si la nueva posición está bloqueada
        if (blockedPositions.Contains(newPosition))
        {
            Debug.Log("No puedes moverte hacia esa posición porque está bloqueada.");
            return; // Cancela el movimiento
        }

        // Detecta si hay una caja en la posición objetivo
        Collider2D boxCollider = Physics2D.OverlapCircle(newPosition, 0.1f, boxLayer);
        if (boxCollider != null)
        {
            // Intenta empujar la caja
            if (TryPushBox(boxCollider.transform, direction))
            {
                // Mueve al jugador a la posición de la caja anterior
                targetPosition = newPosition;
                StartCoroutine(MoveToPosition(targetPosition));
            }
        }
        else if (!Physics2D.OverlapCircle(newPosition, 0.1f, obstacleLayer))
        {
            // Si no hay caja ni obstáculos, mueve al jugador
            targetPosition = newPosition;

            StartCoroutine(MoveToPosition(targetPosition));
        }
    }

    bool TryPushBox(Transform boxTransform, Vector2 direction)
    {
        Vector2 boxNewPosition = (Vector2)boxTransform.position + direction * moveDistance;

        // Detecta si hay una meta en la nueva posición
        Collider2D metaCollider = Physics2D.OverlapCircle(boxNewPosition, 0.1f);
        if (metaCollider != null && metaCollider.CompareTag("Meta"))
        {
            // Si la caja llega a una meta:
            Destroy(metaCollider.gameObject); // Destruye la meta
            StartCoroutine(HandleBoxDestruction(boxTransform, boxNewPosition)); // Maneja la destrucción de la caja
            targetPosition = boxTransform.position; // Mueve al jugador a la posición inicial de la caja
            StartCoroutine(MoveToPosition(targetPosition)); // Inicia el movimiento
            score++; // Incrementa la puntuación
            //UpdateScoreUI(); // Actualiza la puntuación
            soundManager.PlaySound(soundManager.goalSound); // Reproduce el sonido de meta
            return false; // No se mueve la caja, ya que desaparece
        }

        // Si no hay meta, mueve la caja normalmente
        if (!Physics2D.OverlapCircle(boxNewPosition, 0.1f, obstacleLayer | boxLayer))
        {
            StartCoroutine(MoveToPosition(boxTransform, boxNewPosition));
            return true;
        }

        return false; // No se puede mover la caja
    }

    IEnumerator MoveToPosition(Vector2 newPosition)
    {
        isMoving = true; // Bloquea el movimiento
        while ((Vector2)transform.position != newPosition)
        {
            transform.position = Vector2.MoveTowards(transform.position, newPosition, Time.deltaTime * 5f);
            yield return null;
        }
        isMoving = false; // Desbloquea el movimiento
    }

    IEnumerator MoveToPosition(Transform obj, Vector2 newPosition)
    {
        while ((Vector2)obj.position != newPosition)
        {
            obj.position = Vector2.MoveTowards(obj.position, newPosition, Time.deltaTime * 5f);
            yield return null;
        }
    }

    IEnumerator HandleBoxDestruction(Transform box, Vector2 newPosition)
    {
        blockedPositions.Add(new Vector2(box.position.x, box.position.y));

        while ((Vector2)box.position != newPosition)
        {
            box.position = Vector2.MoveTowards(box.position, newPosition, Time.deltaTime * 5f);
            yield return null;
        }

        SpriteRenderer boxRenderer = box.GetComponent<SpriteRenderer>();
        if (boxRenderer != null)
        {
            float fadeDuration = 1.0f;
            float elapsedTime = 0f;
            Color initialColor = boxRenderer.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                boxRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                yield return null;
            }
        }

        blockedPositions.Remove(new Vector2(box.position.x, box.position.y));
        Destroy(box.gameObject);
        //UpdateScoreUI();

        if (score >= totalBoxes)
        {
            CompleteLevel();
        }
    }

    /*
    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Cajas: {score} / {totalBoxes}";
        else
            Debug.LogWarning("ScoreText no está asignado en el inspector.");
    }
    */

    void CompleteLevel()
    {
        Debug.Log("¡Nivel Completado!");
        levelComplete = true; // Indica que el nivel está completo
        StartCoroutine(ShowLevelCompleteImage()); // Inicia la animación del PNG
        soundManager.PlaySound(soundManager.levelCompleteSound); // Reproduce el sonido de nivel completado

    }

    IEnumerator ShowLevelCompleteImage()
    {
        GameObject levelCompleteImage = Instantiate(levelCompleteImagePrefab, transform.position, Quaternion.identity);
        Transform imageTransform = levelCompleteImage.transform;
        imageTransform.localScale = new Vector3(1f, 1f, 1f);

        Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        screenCenter.z = 0f;

        while (imageTransform.localScale.x < finalSize.x || imageTransform.position != screenCenter)
        {
            imageTransform.localScale = Vector3.MoveTowards(imageTransform.localScale, new Vector3(finalSize.x, finalSize.y, 1f), Time.deltaTime * growSpeed);
            imageTransform.position = Vector3.MoveTowards(imageTransform.position, screenCenter, Time.deltaTime * growSpeed);
            yield return null;
        }

        // Esperar para reiniciar el nivel
        yield return WaitForAnyKeyPress();
    }

    IEnumerator WaitForAnyKeyPress()
    {
        while (!Input.anyKeyDown)
        {
            yield return null; // Espera a que el jugador presione una tecla
        }
        RestartLevel(); // Reinicia el nivel al presionar cualquier tecla
    }

    void RestartLevel()
    {
        // Reinicia la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void StartGame()
    {
        gameStarted = true; // Marca que el jugador ha iniciado el juego
        fixedImage.enabled = false; // Desactiva la imagen fija
        blinkingImage.enabled = false; // Desactiva la imagen parpadeante
        backgroundImage.enabled = false; // Desactiva el fondo
    }

    IEnumerator BlinkStartImage()
    {
        while (!gameStarted)
        {
            blinkingImage.enabled = !blinkingImage.enabled; // Alterna la visibilidad de la imagen
            yield return new WaitForSeconds(0.5f); // Parpadea cada 0.5 segundos
        }
    }
}
