using UnityEngine;
using System.Collections;

public class ActorController : MonoBehaviour {

    public Vector3 velocity;
    public BoxCollider2D platBox;
    bool inAir;
    public float gravityValue;
    private Vector3 gravity;
    public float maxX;
    public float maxY;
    private Vector3 upward, ahead;
    private float speed;
    public float maxSpeed;

    private void findGround(RaycastHit2D castInfo, float initDist = Mathf.Infinity, float offsetX = 0.0f, float offsetY = 0.0f)
    {
        var boxSize = new Vector2(platBox.size.x * transform.localScale.x, platBox.size.y * transform.localScale.y);
        Vector3 norm = upward = Vector3.up;
        float normAngle = Mathf.Atan2(norm.y, norm.x);
        float normAngleDeg = normAngle * Mathf.Rad2Deg;
        float rot = normAngle - Mathf.PI / 2.0f;
        float rotDeg = normAngleDeg - 90.0f;
        ahead.x = Mathf.Cos(rot);
        ahead.y = Mathf.Sin(rot);
        ahead.Normalize();
        //print(norm);
        //print(ahead);
        //platBox.transform.RotateAround(castInfo.point, Vector3.forward, rotDeg - platBox.transform.eulerAngles.z);
        var rotboxOffset = (Vector3)(ahead * platBox.offset.x + upward * platBox.offset.y);

        var boxCenter = platBox.transform.position + rotboxOffset;
        var upperOrigin = boxCenter + upward * boxSize.y * 0.15f + new Vector3(offsetX, offsetY);
        var mask = LayerMask.GetMask("Terrain");
        var boxCastInfo = Physics2D.BoxCast(upperOrigin, boxSize, 0/*rotDeg*/, -norm, initDist, mask);
        if (!boxCastInfo.collider) return;
        float minDist = 0.0f, maxDist = boxCastInfo.distance;
        var diff = Mathf.Abs(maxDist - minDist);
        if (diff > 0.01f)
        {
            while (diff > 0.01)
            {
                var curDist = (maxDist + minDist) / 2.0f;
                boxCastInfo = Physics2D.BoxCast(upperOrigin, boxSize, 0/*rotDeg*/, -norm, curDist, mask);
                if (boxCastInfo.collider)
                {
                    maxDist = curDist;
                }
                else
                {
                    minDist = curDist;
                }
                diff = Mathf.Abs(maxDist - minDist);
            }
        }
        platBox.transform.position = upperOrigin - norm * minDist - rotboxOffset;

    }

    void handleAir()
    {
        var boxSize = new Vector2(platBox.size.x * transform.localScale.x, platBox.size.y * transform.localScale.y);
        //transform.Rotate(0, 0, -transform.eulerAngles.z);
        //print("INAIR");
        var right = Input.GetKey(KeyCode.RightArrow);
        var left = Input.GetKey(KeyCode.LeftArrow);
        if (right != left)
        {
            this.velocity.x = 3.0f;
            if (left) this.velocity.x = -this.velocity.x;
        }
        else this.velocity.x = 0.0f;
        this.velocity += gravity * Time.deltaTime;
        this.velocity.x = Mathf.Clamp(this.velocity.x, -maxX, maxX);
        this.velocity.y = Mathf.Clamp(this.velocity.y, -maxY, maxY);
        var velocity = this.velocity * Time.deltaTime;
        //print("V :" + velocity.x + " _ " + velocity.y);
        speed = 0;
        float angle = platBox.transform.eulerAngles.z;
        var mask = LayerMask.GetMask("Terrain");
        var velocityDirection = velocity.normalized;
        var velocityMag = velocity.magnitude;
        var boxCenter = platBox.transform.position + (Vector3)platBox.offset;
        var boxCastInfo = Physics2D.BoxCast(boxCenter, boxSize, 0/*rotDeg*/, velocityDirection, velocityMag, mask);
        var origCastInfo = boxCastInfo;
        if (boxCastInfo.collider != null)
        {
            print("COLLIDE");
            float minFrac = 0, maxFrac = boxCastInfo.fraction, diff = Mathf.Abs(maxFrac - minFrac);
            Vector3 newPos = platBox.transform.position;

            while (diff > 0.05)
            {
                var curFrac = (maxFrac + minFrac) / 2.0f;
                newPos = platBox.transform.position + curFrac * velocity;
                var xPos = platBox.transform.position + curFrac * new Vector3(velocity.x, 0);
                var yPos = platBox.transform.position + curFrac * new Vector3(0, velocity.y);
                if (Physics2D.OverlapBox(platBox.offset + (Vector2)newPos, boxSize, 0, mask) ||
                    (Physics2D.OverlapBox(platBox.offset + (Vector2)xPos, boxSize, 0, mask) &&
                    Physics2D.OverlapBox(platBox.offset + (Vector2)yPos, boxSize, 0, mask)))
                {
                    maxFrac = curFrac;
                }
                else
                {
                    minFrac = curFrac;
                }
                diff = Mathf.Abs(maxFrac - minFrac);
            }
            newPos = platBox.transform.position + minFrac * velocity;
            platBox.transform.position = newPos - (Vector3)platBox.offset;
            boxCenter = newPos;

            var remainingVel = velocity * (1 - minFrac);
            //print("minFrac: " + minFrac);
            //print("velocity: " + velocity);
            //print("Remaining velocity: " + remainingVel); 
            //print("post-move collision? " + Physics2D.OverlapBox((Vector2)newPos + platBox.offset, boxSize, 0, mask));
            //print("post-move collision2? " + Physics2D.OverlapBox(new Vector2(newPos.x,0) + platBox.offset, boxSize, 0, mask));
            //print("post-move collision3? " + Physics2D.OverlapBox(new Vector2(0,newPos.y) + platBox.offset, boxSize, 0, mask));

            if (velocity.x != 0.0f && velocity.y != 0.0f)
            {


                float horzDist = Mathf.Max(1.0f, Mathf.Abs(remainingVel.x));
                float vertDist = Mathf.Max(1.0f, Mathf.Abs(remainingVel.y));
                Vector2 horzDir = new Vector2(Mathf.Sign(remainingVel.x), 0);
                Vector2 vertDir = new Vector2(0, Mathf.Sign(remainingVel.y));
                //print("H CastD: " + horzDist);
                //print("V CastD: " + vertDist);


                var horzCast = Physics2D.BoxCast(boxCenter, boxSize,
                    angle, horzDir, horzDist, mask);
                var vertCast = Physics2D.BoxCast(boxCenter, boxSize,
                    angle, vertDir, vertDist, mask);

                //print("H Cast: " + horzCast.collider);
                //print("V Cast: " + vertCast.collider);

                if (vertCast.collider && vertCast.distance <= Mathf.Abs(remainingVel.y))
                {
                    //print("Blocked on Vert");
                    //print(vertCast.distance + " " + remainingVel.y);
                    var direction = new Vector2(0, Mathf.Sign(remainingVel.y));
                    float minDist = 0.0f, maxDist = vertCast.distance, diffDist = Mathf.Abs(maxDist - minDist);
                    while (diffDist > 0.05)
                    {
                        
                        float curDist = (maxDist + minDist) / 2.0f;
                        newPos = platBox.transform.position + curDist * (Vector3)direction;
                        if (Physics2D.OverlapBox(platBox.offset + (Vector2)newPos, boxSize, 0, mask))
                        {
                            maxDist = curDist;
                        }
                        else
                        {
                            minDist = curDist;
                        }
                        diffDist = (Mathf.Abs(maxDist - minDist));
                    }
                    newPos = platBox.transform.position + minDist * (Vector3)direction;
                    platBox.transform.position = newPos - (Vector3)platBox.offset;
                    inAir = velocity.y > 0.0f;
                    velocity.x = velocity.y = 0.0f;
                    //findGround(vertCast, vertCast.distance);


                }
                else if (horzCast.collider && horzCast.distance <= Mathf.Abs(remainingVel.x))
                {
                    velocity.x = 0;
                    velocity.y = remainingVel.y;
                    print(vertCast.distance + " " + remainingVel.x);
                    //print("Blocked on Horz");
                }
                else
                {
                    //print("Found no block?");
                    velocity.y = 0;
                    velocity.x = remainingVel.x;
                    inAir = false;
                }

            }
            else
            {
                if (velocity.y != 0.0f)
                {
                    var direction = new Vector2(0, Mathf.Sign(remainingVel.y));
                    var vertCast = Physics2D.BoxCast(boxCenter, boxSize,
                        angle, direction, Mathf.Max(1.0f, Mathf.Abs(remainingVel.y)), mask);
                    
                    print("V Cast: " + vertCast.collider);

                    if (vertCast.collider && vertCast.distance <= Mathf.Abs(remainingVel.y))
                    {
                        print("Blocked on Vert");
                        print(vertCast.distance + " " + remainingVel.y);
                        float minDist = 0.0f, maxDist = vertCast.distance, diffDist = Mathf.Abs(maxDist - minDist);
                        while (diffDist > 0.03)
                        {
                            float curDist = (maxDist + minDist) / 2.0f;
                            newPos = platBox.transform.position + curDist * (Vector3)direction;
                            if (Physics2D.OverlapBox(platBox.offset + (Vector2)newPos, boxSize, 0, mask))
                            {
                                maxDist = curDist;
                            }
                            else
                            {
                                minDist = curDist;
                            }
                            diffDist = (Mathf.Abs(maxDist - minDist));
                        }
                        newPos = platBox.transform.position + minDist * (Vector3)direction;
                        platBox.transform.position = newPos - (Vector3)platBox.offset;
                        print("Post Move: " + Physics2D.OverlapBox(platBox.offset + (Vector2)newPos, boxSize, 0, mask));
                        if (velocity.y < 0.0f)
                            inAir = false;
                        velocity.y = 0.0f;
                    }
                }
            }
        }
        platBox.transform.position += velocity;

    }

    void handleGround()
    {

        var boxSize = new Vector2(platBox.size.x * transform.localScale.x, platBox.size.y * transform.localScale.y);
        var right = Input.GetKey(KeyCode.RightArrow);
        var left = Input.GetKey(KeyCode.LeftArrow);
        var jmpB = Input.GetKey(KeyCode.Z);
        speed = 0.0f;
        if (right != left)
        {
            speed = 3.0f;
            if (left) speed = -speed;
            //print(ahead);
        }
        if (jmpB)
        {
            inAir = true;
            velocity.y = 3.0f;
        }
        else
        {
            var rotboxOffset = (Vector3)(ahead * platBox.offset.x + upward * platBox.offset.y);
            var speedMulDT = speed * Time.deltaTime;
            var mask = LayerMask.GetMask("Terrain");
            var boxCenter = platBox.transform.position + rotboxOffset;
            var upperOrigin = boxCenter + Vector3.up * boxSize.y * 0.15f;
            upperOrigin += ahead * speedMulDT;
            float upAngle = Mathf.Atan2(upward.y, upward.x) * Mathf.Rad2Deg;
            float rot = upAngle - 90.0f;
            if (!Physics2D.OverlapBox(upperOrigin, boxSize, 0, mask)) {
                var boxCastInfo = Physics2D.BoxCast(upperOrigin, boxSize, 0/*rotDeg*/, Vector3.down, boxSize.y * 0.30f, mask);
                if (boxCastInfo.collider)
                {
                    if (boxCastInfo.fraction > 0.0f)
                    {
                        var movVec = ahead * speedMulDT;
                        float minDist = 0.0f, maxDist = boxCastInfo.distance, diff = Mathf.Abs(maxDist - minDist);
                        while (diff > 0.01)
                        {
                            float curDist = (maxDist + minDist) / 2.0f;
                            if (Physics2D.OverlapBox(upperOrigin + curDist * Vector3.down, boxSize, 0.0f, mask))
                            {
                                maxDist = curDist;
                            }
                            else
                            {
                                minDist = curDist;
                            }
                            diff = Mathf.Abs(maxDist - minDist);
                        }
                        platBox.transform.position = upperOrigin + minDist * Vector3.down - rotboxOffset;
                    }
                }

                else if (!Physics2D.OverlapBox(upperOrigin, boxSize, 0, mask))
                {
                    inAir = true;
                    platBox.transform.position += ahead * speedMulDT;
                }
            }
        }
        
    }

    // Use this for initialization
    void Start() {
        velocity = new Vector2();
        inAir = true;
        gravity = new Vector2(0, gravityValue);
        maxX = 6.0f;
        maxY = 10.0f;
        maxSpeed = 10.0f;
        upward = new Vector3(0, 1);
        ahead = new Vector3(1, 0);
        if (!platBox)
        {
            platBox = GetComponent<BoxCollider2D>();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (inAir)
        {
            handleAir();
        }
        else
        {
            handleGround();
        }
        
    }
}
