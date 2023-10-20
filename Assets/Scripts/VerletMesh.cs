using System.Collections.Generic;
using System.Drawing;
using UnityEngine; 

public class VerletMesh : MonoBehaviour
{

    public Material material;

    [SerializeField]
    private Mesh mesh;

    [SerializeField]
    private float gravity = -0.24f;
    [SerializeField]
    private float friction = 0.99f;
    [SerializeField]
    private float damping = 1f;
    [SerializeField]
    private float lineWidth = 0.04f;


    private List<GameObject> spheres;
    private List<Particle> particles;
    private List<Connector> connectors;

    private Vector3 lastMousePos = Vector3.zero;
    private int grabbedParticle = -1;
    [SerializeField]
    private float breakLength = 3.0f;

    // Initalize
    void Start()
    {
        Vector3 spawnParticlePos = Vector3.zero;

        spheres = new List<GameObject>();
        particles = new List<Particle>();
        connectors = new List<Connector>();

        for(int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 vertexPosition = mesh.vertices[i];

            // Create a sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.position = vertexPosition;
            sphere.transform.localScale = new Vector3(lineWidth, lineWidth, lineWidth);

            // Create particle
            Particle point = new Particle();
            point.pinnedPos = vertexPosition;
            point.oldPos = vertexPosition;
            point.pos = vertexPosition;

            if (mesh.colors.Length > 0)
            {
                point.pinned = mesh.colors[i].r > 0.5f;
            }

            // Add particle and spehere to lists
            spheres.Add(sphere);
            particles.Add(point);
        }
        var submeshIndex = 0;

        int[] triIndices = mesh.GetTriangles(submeshIndex);
        for (int i = 0; i < triIndices.Length - 1; i++) // triIndices.Length;i++)
        {
            //loop back to the first index if we've completed three entries.
            int endOfEdge = (i % 3 == 2) ? -2 : 1;

            //if (endOfEdge == 0) continue;

            var gameObject = new GameObject("Line" + i);
            var line = gameObject.AddComponent<LineRenderer>();
            Connector connector = new Connector();

            connector.p0 = spheres[triIndices[i]];
            connector.p1 = spheres[triIndices[i + endOfEdge]];

            connector.point0 = particles[triIndices[i]];
            connector.point1 = particles[triIndices[i + endOfEdge]];

            connector.point0.pos = connector.p0.transform.position;
            connector.point0.oldPos = connector.p0.transform.position;

            connector.point1.pos = connector.p1.transform.position;
            connector.point1.oldPos = connector.p1.transform.position;

            connector.lineRender = line;
            connector.lineRender.material = material;

            connector.startDistance = Vector3.Distance(connector.point0.pos, connector.point1.pos);
            connector.breakDistance = connector.startDistance * breakLength;


            connectors.Add(connector);

            // Set points for the lines
            var points = new Vector3[2];
            points[0] = connector.p0.transform.position;
            points[1] = connector.p1.transform.position;

            // Draw lines
            connector.lineRender.startWidth = lineWidth;
            connector.lineRender.endWidth = lineWidth;
            connector.lineRender.SetPositions(points);

        }
    }

    
    private void FixedUpdate()
    {

        // Handle mouse input
        Vector3 mousePos = Input.mousePosition;

        Vector3 mouseDelta = mousePos - lastMousePos;

        //Vector3 mousePos_new = Camera.main.ScreenToWorldPoint(mousePos);
        if (Input.GetMouseButton(0))
        {
            for (int i = 0; i < connectors.Count; i++)
            {
                float dist = Vector3.Distance(mousePos, Camera.main.WorldToScreenPoint(connectors[i].point0.pos));
                if (dist <= 30.05f)
                {
                    connectors[i].enabled = false;
                }
            }
        }

        if (Input.GetMouseButton(1) && grabbedParticle == -1)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                float dist = Vector3.Distance(mousePos, Camera.main.WorldToScreenPoint(particles[i].pos));
                if (dist <= 30.05f)
                {
                    lastMousePos = mousePos;
                    grabbedParticle = i;
                    break;
                }
            }
        }
        else if (Input.GetMouseButton(1) && grabbedParticle != -1)
        {
            particles[grabbedParticle].pos += (Camera.main.transform.rotation * (mouseDelta / 100.0f));
        }
        else
        {
            grabbedParticle = -1;
        }


        for (int i = 0; i < connectors.Count; i++)
        {
            float dist1 = Vector3.Distance(connectors[i].point0.pos, connectors[i].point1.pos);
            if (dist1 > connectors[i].breakDistance)
            {
                connectors[i].enabled = false;
            }
        }

        // Update particle positions
        for (int p = 0; p < particles.Count; p++)
        {
            Particle point = particles[p];
            if (point.pinned == true)
            {
                point.pos = point.pinnedPos;
                point.oldPos = point.pinnedPos;
            }
            else
            {
                point.vel = (point.pos - point.oldPos) * friction;
                point.oldPos = point.pos;

                point.pos += point.vel;
                point.pos.y += gravity * Time.fixedDeltaTime;
            }


        }

        // Constraint the points together
        for (int i = 0; i < connectors.Count; i++)
        {
            if (connectors[i].enabled == false)
            {
                Destroy(connectors[i].lineRender);
            }

            else
            {
                float dist = (connectors[i].point0.pos - connectors[i].point1.pos).magnitude;
                float error = Mathf.Abs(dist - connectors[i].startDistance);

                if (dist > connectors[i].startDistance)
                {
                    connectors[i].changeDir = (connectors[i].point0.pos - connectors[i].point1.pos).normalized;
                }
                else if (dist < connectors[i].startDistance)
                {
                    connectors[i].changeDir = (connectors[i].point1.pos - connectors[i].point0.pos).normalized;
                }

                Vector3 changeAmount = connectors[i].changeDir * error;
                connectors[i].point0.pos -= changeAmount * 0.5f;
                connectors[i].point1.pos += changeAmount * 0.5f;

            }
        }

        // Set spheres
        for (int p = 0; p < particles.Count; p++)
        {
            Particle point = particles[p];
            spheres[p].transform.position = new Vector3(point.pos.x, point.pos.y, point.pos.z);
        }

        // Set lines
        for (int i = 0; i < connectors.Count; i++) // every third line will render
        {   
            if (connectors[i].enabled == false)
            {
                Destroy(connectors[i].lineRender);
            }

            else
            {
                // Set points for the lines
                var points = new Vector3[2];
                points[0] = connectors[i].p0.transform.position;
                points[1] = connectors[i].p1.transform.position;

                // Draw lines
                connectors[i].lineRender.startWidth = lineWidth;
                connectors[i].lineRender.endWidth = lineWidth;
                connectors[i].lineRender.SetPositions(points);

            }

        }
    }

    
    public class Connector
    {
        public bool enabled = true;
        public LineRenderer lineRender;
        public GameObject p0;
        public GameObject p1;
        public Particle point0;
        public Particle point1;
        public Vector3 changeDir;
        public float startDistance;
        public float breakDistance;
    }

    public class Particle
    {
        public bool pinned = false;
        public Vector3 pinnedPos;
        public Vector3 pos;
        public Vector3 oldPos;
        public Vector3 vel;
    }

}
