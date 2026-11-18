using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _numOfSegments = 40;
    [SerializeField] private float _segmentLength = 0.05f;
    [SerializeField] private Transform _boatAnchor;

    [Header("Physics")]
    [SerializeField] private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);
    [SerializeField] private float _damping = 0.99f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.015f;
    [SerializeField] private float _friction = 0.7f;

    [Header("Self Collision")]
    [SerializeField] private bool _useSelfCollision = true;
    [SerializeField] private float _selfCollisionDistance = 0.04f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 10;
    [SerializeField] private int _collisionInterval = 2;

    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();
    private Vector3[] _ropePositions;
    private Collider[] _hits = new Collider[8];

    public struct RopeSegment
    {
        public Vector3 currentPosition;
        public Vector3 oldPosition;
        public Transform pinTarget;
        public Vector3 pinOffset;
        public bool touching;
        public Vector3 contactNormal;

        public RopeSegment(Vector3 pos)
        {
            currentPosition = pos;
            oldPosition = pos;
            pinTarget = null;
            pinOffset = Vector3.zero;
            touching = false;
            contactNormal = Vector3.zero;
        }

        public bool IsPinned()
        {
            return pinTarget != null;
        }
    }

private void Awake()
{
    _lineRenderer = GetComponent<LineRenderer>();
    _lineRenderer.useWorldSpace = true;
    _lineRenderer.positionCount = _numOfSegments;
    _ropePositions = new Vector3[_numOfSegments];
    _ropeSegments = new List<RopeSegment>();

    if (_boatAnchor == null)
    {
        Debug.LogWarning("RopeVerlet: _boatAnchor not assigned.", this);
        return;
    }

    Vector3 startPoint = _boatAnchor.position;
    Vector3 direction  = Vector3.down;

    for (int i = 0; i < _numOfSegments; i++)
    {
        _ropeSegments.Add(new RopeSegment(startPoint + direction * (_segmentLength * i)));
    }

    GrabSegment(0, _boatAnchor);
}
    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _numOfConstraintRuns; i++)
        {
            ApplyPins();
            ApplyConstraints();

            if (_useSelfCollision)
            {
                HandleSelfCollision();
            }

            if (i % _collisionInterval == 0)
            {
                HandleCollisions();
            }
        }

        ApplyFriction();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    public int GetClosestSegment(Vector3 position, float maxDistance)
    {
        int closest = -1;
        float closestDistance = maxDistance;

        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            float distance = Vector3.Distance(_ropeSegments[i].currentPosition, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = i;
            }
        }

        return closest;
    }

    public void GrabSegment(int index, Transform hand)
    {
        RopeSegment segment = _ropeSegments[index];
        segment.pinTarget = hand;
        segment.pinOffset = hand.InverseTransformPoint(segment.currentPosition);
        _ropeSegments[index] = segment;
    }

    public void ReleaseSegments(Transform hand)
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            if (segment.pinTarget == hand)
            {
                segment.pinTarget = null;
                _ropeSegments[i] = segment;
            }
        }
    }

    private void Simulate()
    {
        float dt = Time.fixedDeltaTime;

        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            segment.touching = false;

            if (segment.IsPinned())
            {
                segment.oldPosition = segment.currentPosition;
                segment.currentPosition = segment.pinTarget.TransformPoint(segment.pinOffset);
            }
            else
            {
                Vector3 velocity = (segment.currentPosition - segment.oldPosition) * _damping;
                segment.oldPosition = segment.currentPosition;
                segment.currentPosition += velocity;
                segment.currentPosition += _gravity * dt * dt;
            }

            _ropeSegments[i] = segment;
        }
    }

    private void ApplyPins()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            if (segment.IsPinned())
            {
                segment.currentPosition = segment.pinTarget.TransformPoint(segment.pinOffset);
                _ropeSegments[i] = segment;
            }
        }
    }

    private void ApplyConstraints()
    {
        for (int i = 0; i < _numOfSegments - 1; i++)
        {
            RopeSegment currentSeg = _ropeSegments[i];
            RopeSegment nextSeg = _ropeSegments[i + 1];

            Vector3 offset = currentSeg.currentPosition - nextSeg.currentPosition;
            float dist = offset.magnitude;
            if (dist < 0.0001f)
            {
                continue;
            }

            float difference = dist - _segmentLength;
            Vector3 changeVector = (offset / dist) * difference;

            if (currentSeg.IsPinned() && nextSeg.IsPinned())
            {
                continue;
            }
            else if (currentSeg.IsPinned())
            {
                nextSeg.currentPosition += changeVector;
            }
            else if (nextSeg.IsPinned())
            {
                currentSeg.currentPosition -= changeVector;
            }
            else
            {
                currentSeg.currentPosition -= changeVector * 0.5f;
                nextSeg.currentPosition += changeVector * 0.5f;
            }

            _ropeSegments[i] = currentSeg;
            _ropeSegments[i + 1] = nextSeg;
        }
    }

    private void HandleSelfCollision()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            for (int j = i + 3; j < _ropeSegments.Count; j++)
            {
                RopeSegment a = _ropeSegments[i];
                RopeSegment b = _ropeSegments[j];

                Vector3 offset = a.currentPosition - b.currentPosition;
                float dist = offset.magnitude;

                if (dist >= _selfCollisionDistance || dist < 0.0001f)
                {
                    continue;
                }

                Vector3 pushVector = (offset / dist) * (_selfCollisionDistance - dist);

                if (a.IsPinned() && b.IsPinned())
                {
                    continue;
                }
                else if (a.IsPinned())
                {
                    b.currentPosition -= pushVector;
                }
                else if (b.IsPinned())
                {
                    a.currentPosition += pushVector;
                }
                else
                {
                    a.currentPosition += pushVector * 0.5f;
                    b.currentPosition -= pushVector * 0.5f;
                }

                _ropeSegments[i] = a;
                _ropeSegments[j] = b;
            }
        }
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];

            if (segment.IsPinned())
            {
                continue;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(segment.currentPosition, _collisionRadius, _hits, _collisionMask);

            for (int h = 0; h < hitCount; h++)
            {
                Collider collider = _hits[h];
                Vector3 closestPoint = collider.ClosestPoint(segment.currentPosition);
                float distance = Vector3.Distance(segment.currentPosition, closestPoint);

                Vector3 normal;
                float depth;

                if (distance > 0.0001f)
                {
                    normal = (segment.currentPosition - closestPoint).normalized;
                    depth = _collisionRadius - distance;
                }
                else
                {
                    normal = (segment.currentPosition - collider.bounds.center).normalized;
                    depth = _collisionRadius;
                }

                if (depth > 0f)
                {
                    segment.currentPosition += normal * depth;
                    segment.touching = true;
                    segment.contactNormal = normal;
                }
            }

            _ropeSegments[i] = segment;
        }
    }

    private void ApplyFriction()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];

            if (segment.touching && !segment.IsPinned())
            {
                Vector3 velocity = segment.currentPosition - segment.oldPosition;
                Vector3 normalPart = segment.contactNormal * Vector3.Dot(velocity, segment.contactNormal);
                Vector3 sidewaysPart = velocity - normalPart;

                segment.oldPosition = segment.currentPosition - sidewaysPart * (1f - _friction);
                _ropeSegments[i] = segment;
            }
        }
    }

    private void DrawRope()
    {
        for (int i = 0; i < _numOfSegments; i++)
        {
            _ropePositions[i] = _ropeSegments[i].currentPosition;
        }
        _lineRenderer.SetPositions(_ropePositions);
    }

       // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
    }
    
}