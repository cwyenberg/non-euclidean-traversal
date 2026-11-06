
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProceduralCanvas : MonoBehaviour
{

    // Objects that will exist on the screen
    private SoccerBall soccerBall;
    private Bullet bullet;

    // Materials and sprites for drawing
    private Material lineMaterial;

    // Configurable fields
    [SerializeField] private float edgeWidth = 0.035f;
    [SerializeField] private int numCovers = 1;
    [SerializeField] private int curveSegments = 10;


    private void Awake()
    {

        // Material for visible 2D lines.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find Sprites/Default shader.");
            return;
        }

        lineMaterial = new Material(shader);

    }

    private void Start()
    {
        if (lineMaterial == null) return;

        // Instantiate the soccer ball with its own builder.
        // Initialization is also called within the constructor
        soccerBall = new SoccerBall(
            transform: transform,
            lineMaterial: lineMaterial,
            edgeWidth: edgeWidth,
            numCovers: numCovers,
            curveSegments: curveSegments
        );

        // // Instantiate the bullet with its own builder.
        // bullet = new Bullet(
        //     transform: transform,
        //     lineMaterial: lineMaterial,
        //     edgeWidth: edgeWidth,
        //     numCovers: numCovers,
        //     curveSegments: curveSegments
        // );

    }



    void LateUpdate()
    {

        if (PlayerMovement.hasMoved)
        {

            soccerBall.Update();
            // bullet.Update();

        }
    }


    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);

    }
}
