using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// --- VERLET ROPES ---
[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    // rope
    [SerializeField] private int _ropeSegmentCount = 20;
    [SerializeField] private float _segmentLength = 1f;

    // physics values
    [SerializeField] private Vector2 _gravityForce = new Vector2(0, 2f);
    [SerializeField] private float _airDampingFactor = 0.90f;

    // constraints
    [SerializeField] private int _iterationCount = 50;

    private LineRenderer _lineRenderer;
    private readonly List<RopeSegment> _ropeSegments = new List<RopeSegment>();
    private Vector2 _startPosition;
    private Vector2 _currentHingePosition;
    private Vector2 _mousePosition;
    private bool _isAttachedToHinge = false;
    private readonly List<Hinge> _hinges = new List<Hinge>();

    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 FormerPosition;

        public RopeSegment(Vector2 pos)
        {
            CurrentPosition = pos;
            FormerPosition = pos;
        }
    }

    public struct Hinge
    {
        public Bounds HingeBounds;

        public Hinge(Bounds hingeBounds)
        {
            HingeBounds = hingeBounds;
        }
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _ropeSegmentCount;

        _startPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        for (int i = 0; i < _ropeSegmentCount; i++)
        {
            _ropeSegments.Add(new RopeSegment(_startPosition));
            _startPosition.y -= _segmentLength;
        }

        GetHinges();
    }

    private void Update()
    {
        GetInputs();
        AttachToHinge();
        DrawRope();
    }

    private void GetInputs()
    {
        _mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void FixedUpdate()
    {
        SimulateRope();

        // iterate and apply the constraints
        for (int i = 0; i < _iterationCount; i++)
        {
            ApplyConstraints();
        }
    }

    private void GetHinges()
    {
        // get all the hinge objects in scene
        GameObject[] hingeArray = GameObject.FindGameObjectsWithTag("Hinge");

        foreach (GameObject hingeObject in hingeArray)
        {
            // get hinge collider
            CircleCollider2D hingeCol = hingeObject.GetComponent<CircleCollider2D>();
            Bounds hingeBounds = hingeCol.bounds;

            Hinge hinge = new Hinge(hingeBounds);
            _hinges.Add(hinge);
        }
    }

    private void AttachToHinge()
    {
        if (_isAttachedToHinge)
        {
            return;
        }

        foreach (Hinge hinge in _hinges)
        {
            // check if the mouse position is in the hinge bounds
            if (hinge.HingeBounds.min.x < _mousePosition.x && hinge.HingeBounds.max.x > _mousePosition.x && hinge.HingeBounds.min.y < _mousePosition.y && _mousePosition.y < hinge.HingeBounds.max.y)
            {
                _isAttachedToHinge = true;
                _currentHingePosition = hinge.HingeBounds.center;
            }
        }
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_ropeSegmentCount];

        for (int i = 0; i < _ropeSegmentCount; i++)
        {
            ropePositions[i] = _ropeSegments[i].CurrentPosition;
        }

        _lineRenderer.SetPositions(ropePositions);
    }

    private void SimulateRope()
    {
        for (int i = 0; i < _ropeSegmentCount; i++)
        {
            RopeSegment ropeSegment = _ropeSegments[i];
            Vector2 velocity = (ropeSegment.CurrentPosition - ropeSegment.FormerPosition) * _airDampingFactor;

            // set former position to current position
            ropeSegment.FormerPosition = ropeSegment.CurrentPosition;

            // move current position with the velocity amount
            ropeSegment.CurrentPosition += velocity + _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = ropeSegment;
        }
    }

    private void ApplyConstraints()
    {
        RopeSegment firstSegment = _ropeSegments[0];

        if (_isAttachedToHinge)
        {
            // keep first segment attached to hinge
            firstSegment.CurrentPosition = _currentHingePosition;
        }
        else
        {
            // keep first point attached to the mouse position
            firstSegment.CurrentPosition = _mousePosition;
        }

        _ropeSegments[0] = firstSegment;

        for (int i = 0; i < _ropeSegmentCount - 1; i++)
        {
            RopeSegment ropeSegmentUp = _ropeSegments[i];
            RopeSegment ropeSegmentDown = _ropeSegments[i + 1];

            // get the distance between segments
            float distance = (ropeSegmentUp.CurrentPosition - ropeSegmentDown.CurrentPosition).magnitude;

            // get the difference to determine if the rope segment is stretched (shrinked) or not
            float difference = distance - _segmentLength;

            // get the vector of fixing the stretching (shrinking) situation
            Vector2 changeDirection = (ropeSegmentUp.CurrentPosition - ropeSegmentDown.CurrentPosition).normalized;
            Vector2 changeVector = changeDirection * difference;

            // apply the fixing vector to segments
            if (i == 0)
            {
                ropeSegmentDown.CurrentPosition += changeVector;
            }
            else
            {
                ropeSegmentUp.CurrentPosition -= changeVector * 0.5f;
                ropeSegmentDown.CurrentPosition += changeVector * 0.5f;
            }

            _ropeSegments[i] = ropeSegmentUp;
            _ropeSegments[i + 1] = ropeSegmentDown;
        }
    }
}
