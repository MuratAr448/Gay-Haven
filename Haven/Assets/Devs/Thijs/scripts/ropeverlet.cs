using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Simulation;
public class ropeverle : MonoBehaviour
{

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 50;
    [SerializeField] private float _ropeSegmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector3 _gravityForce = new Vector3(0f, -2f);
    [SerializeField] private float _dampingFactor = 0.90f;
    [SerializeField] private LayerMask _collisionmask;
    [SerializeField] private float _colisionradius = 0.1f;
    [SerializeField] private float _bouncefactor = 0.1f;
    [SerializeField] private float _correctionclampamount = 0.1f;


    [Header("Constraints")]
    [SerializeField] private int _numOFConstraintRuns = 50;

    [Header("optimization")]
    [SerializeField] private int _collisionsegmentinterval = 2;



    private LineRenderer _LineRenderer;
    private List<ropesegment> _ropesegments = new List<ropesegment>();

    private Vector3 _ropeStartPoint;

    private void Awake()
    {
        _LineRenderer = GetComponent<LineRenderer>();
        _LineRenderer.positionCount = _numOfRopeSegments;

        _ropeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            _ropesegments.Add(new ropesegment(_ropeStartPoint));
            _ropeStartPoint.y -= _ropeSegmentLength;

        }
    }


    public struct ropesegment
    {
        public Vector3 currentposition;
        public Vector3 oldposition;

        public ropesegment(Vector2 pos)
        {
            currentposition = pos;
            oldposition = pos;
        }

    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        DrawRope();
    }
    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _numOFConstraintRuns; i++)
        {
            {

                applyconstraints();
                if (i % _collisionsegmentinterval == 0)
                    handlecolisions();

            }
        }
    }
    private void Simulate()
    {
        for (int i = 0; i < _ropesegments.Count; i++) {
            ropesegment segment = _ropesegments[i];
            Vector3 velocity = (segment.currentposition - segment.oldposition) * _dampingFactor;
            segment.oldposition = segment.currentposition;
            segment.currentposition += velocity;
            segment.currentposition += _gravityForce * Time.fixedDeltaTime;
            _ropesegments[i] = segment;
        } 
    }
    private void applyconstraints()
    {
        ropesegment firstsegment = _ropesegments[0];
        firstsegment.currentposition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _ropesegments[0] = firstsegment;

        for (int i = 0; i < _numOfRopeSegments -1; i++)
        {
            ropesegment currentSeg = _ropesegments[i];
                ropesegment nextSeg = _ropesegments[i + 1]; 
            float dist = (currentSeg.currentposition - nextSeg.currentposition).magnitude;
            float difference = (dist - _ropeSegmentLength);

            Vector3 changeDir = (currentSeg.currentposition - nextSeg.currentposition).normalized;
            Vector3 changevector = changeDir * difference;


            if (i != 0)
            {
                currentSeg.currentposition -= (changevector * 0.5f);
                nextSeg.currentposition += (changevector * 0.5f);

            }
            else
            {
                nextSeg.currentposition += changevector;
            }
            _ropesegments[i] = currentSeg;
            _ropesegments[i + 1] = nextSeg;

        }

    }


    private void handlecolisions()
    {
        for (int i = 0; i < _ropesegments.Count; i++)
        {
            ropesegment segment = _ropesegments[i];
            Vector3 velocity = segment.currentposition - segment.currentposition;
            Collider[] colliders = Physics.OverlapSphere(segment.currentposition, _colisionradius, _collisionmask);

            foreach (Collider collider in colliders)
            {
                Vector3 closestpoint = collider.ClosestPoint(segment.currentposition);
                float distance = Vector3.Distance(segment.currentposition, closestpoint);

                if (distance < _colisionradius) 

                    { 
                
                Vector3  normal = (segment.currentposition -closestpoint).normalized;

                    if (normal == Vector3. zero)
                    {
                        normal = (segment.currentposition - (Vector3) collider.transform.position).normalized;

                    }


                    float depth = _colisionradius - distance;

                    segment.currentposition += normal * depth;


                   velocity = Vector3.Reflect(velocity, normal) * _bouncefactor;


                }
            }

            segment.oldposition= segment.currentposition - velocity;
            _ropesegments[i] = segment;


        }

    }
    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numOfRopeSegments];
        for (int i = 0;  i < _numOfRopeSegments; i ++)
        {
            ropePositions[i] = _ropesegments[i].currentposition;
        }
        _LineRenderer.SetPositions(ropePositions);
    }
}
