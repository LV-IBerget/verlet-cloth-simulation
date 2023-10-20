﻿using System.Collections.Generic;
using UnityEngine; 

public class Verlet : MonoBehaviour
{
    [SerializeField]
    private int rows = 32;
    [SerializeField]
    private int columns = 32;
    [SerializeField]
    private float spacing = 0.5f;
    public Material material;

    [SerializeField]
    public float gravity = -0.24f;
    [SerializeField]
    public float friction = 0.99f;
    [SerializeField]
    public float damping = 1f;


    private List<GameObject> spheres;
    private List<Particle> particles;
    private List<Connector> connectors;

    // Initalize
    void Start()
    {
        Vector3 spawnParticlePos = new Vector2(0, 0);

        spheres = new List<GameObject>();
        particles = new List<Particle>();
        connectors = new List<Connector>();

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                // Create a sphere
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                var mat = sphere.GetComponent<Renderer>();
                mat.material = material;
                sphere.transform.position = new Vector3(spawnParticlePos.x, 0.0f, spawnParticlePos.y);
                sphere.transform.localScale = new Vector3(0.2f, 0.2f);

                // Create particle
                Particle point = new Particle();
                point.pinnedPos = new Vector3(spawnParticlePos.x, 0.0f, spawnParticlePos.y);

                // Create a vertical connector 
                if (x != 0)
                {
                    LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();

                    Connector connector = new Connector();
                    connector.p0 = sphere;
                    connector.p1 = spheres[spheres.Count - 1];

                    connector.point0 = point;
                    connector.point1 = particles[particles.Count - 1];
                    connector.point0.pos = sphere.transform.position;
                    connector.point0.oldPos = sphere.transform.position;

                    connectors.Add(connector);

                    connector.lineRender = line;
                    connector.lineRender.material = material;
                }

                // Create a horizontal connector
                if (y != 0)
                {
                    LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();

                    Connector connector = new Connector();
                    connector.p0 = sphere;
                    connector.p1 = spheres[(y - 1) * (rows + 1) + x];

                    connector.point0 = point;
                    connector.point1 = particles[(y - 1) * (rows + 1) + x];
                    connector.point0.pos = sphere.transform.position;
                    connector.point0.oldPos = sphere.transform.position;

                    connectors.Add(connector);

                    connector.lineRender = line;
                    connector.lineRender.material = material;

                }

                // Pin the points in the top row of the grid
                if (x == 0 || x == columns-1)
                {
                    point.pinned = true;
                }

                spawnParticlePos.x -= spacing;

                // Add particle and spehere to lists
                spheres.Add(sphere);
                particles.Add(point);


            }

            spawnParticlePos.x = 0;
            spawnParticlePos.y -= spacing;
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
    }

    public class Particle
    {
        public bool pinned = false;
        public Vector3 pinnedPos;
        public Vector3 pos;
        public Vector3 oldPos;
        public Vector3 vel;
    }

    void Update()
    {
        // Handle mouse input
        Vector3 mousePos = Input.mousePosition;
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

        // I should move this to FixedUpdate
        for (int i = 0; i < connectors.Count; i++)
        {
            float dist1 = Vector3.Distance(connectors[i].point0.pos, connectors[i].point1.pos);
            if (dist1 > 1.4)
            {
                connectors[i].enabled = false;
            }
        }
    }

    private void FixedUpdate()
    {
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
        var startDistance = 0.5f;
        for (int i = 0; i < connectors.Count; i++)
        {
            if (connectors[i].enabled == false)
            {
                // Do nothing
            }

            else
            {
                float dist = (connectors[i].point0.pos - connectors[i].point1.pos).magnitude;
                float error = Mathf.Abs(dist - startDistance);

                if (dist > startDistance)
                {
                    connectors[i].changeDir = (connectors[i].point0.pos - connectors[i].point1.pos).normalized;
                }
                else if (dist < startDistance)
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
            spheres[p].transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);

        }

        // Set lines
        for (int i = 0; i < connectors.Count; i++)
        {   
            if (connectors[i].enabled == false)
            {
                Destroy(connectors[i].lineRender);
            }

            else
            {
                // Set points for the lines
                var points = new Vector3[2];
                points[0] = connectors[i].p0.transform.position + new Vector3(0, 0, 1);
                points[1] = connectors[i].p1.transform.position + new Vector3(0, 0, 1);

                // Draw lines
                connectors[i].lineRender.startWidth = 0.04f;
                connectors[i].lineRender.endWidth = 0.04f;
                connectors[i].lineRender.SetPositions(points);

            }

        }
    }
}
