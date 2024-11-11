using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyBehavior {EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    //pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    //properties
    public float speed = 1.0f;
    public float visionDistance = 5;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1; 

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop Moving the enemy if the player has reached the goal
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            //Debug.Log("Enemy stopped since the player has reached the goal or the player is dead");
            return;
        }

        switch(behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }

    }

    public void Reset()
    {
        Debug.Log("enemy reset");
        path.Clear();
        state = EnemyState.DEFAULT;
        currentTile = FindWalkableTile();
        transform.position = currentTile.transform.position;
    }

    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        int randomIndex = 0;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            randomIndex = (int)(Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps Walking in Random direction, Will not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 
                
                //Changed the color to white to differentiate from other enemies
                material.color = Color.white;
                
                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;
                
                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Enemy chases the player when it is nearby
    private void HandleEnemyBehavior2()
    {
        switch (state)
        {
            case EnemyState.DEFAULT:
                // If the player is within vision range, start chasing them
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    state = EnemyState.CHASE;
                }
                else
                {
                    // Otherwise, move randomly
                    material.color = Color.red; // You can change the color to differentiate
                    if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                break;

            case EnemyState.MOVING:
                // Move towards the current target
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // If we have reached the target tile, and we are chasing the player, update the path
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    if (state == EnemyState.CHASE)
                    {
                        // After reaching the target, recalculate the path towards the player’s last known position
                        path.Clear();
                        path = pathFinder.RandomPath(currentTile, 20); // Use random path to simulate chasing
                        if (path.Count > 0)
                        {
                            targetTile = path.Dequeue();
                        }
                        else
                        {
                            state = EnemyState.DEFAULT; // No path to the player, revert to default state
                        }
                    }
                    else
                    {
                        state = EnemyState.DEFAULT; // Revert to default behavior when not chasing
                    }
                }
                break;

            case EnemyState.CHASE:
                // If the player is within vision range, we continue chasing
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    // The enemy chases the last known position of the player
                    targetTile = playerGameObject.GetComponent<Player>().currentTile;
                    path.Clear();
                    path = pathFinder.RandomPath(currentTile, 20);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                else
                {
                    // If the player is no longer within vision range, revert to default state
                    state = EnemyState.DEFAULT;
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Third behavior works similar to behavior two; however, enemy goes to two tiles behind the player.
    private void HandleEnemyBehavior3()
    {
        switch (state)
        {
            case EnemyState.DEFAULT:
                // If the player is within vision range, start chasing them
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    state = EnemyState.CHASE;
                }
                else
                {
                    // Otherwise, move randomly
                    material.color = Color.red; // You can change the color to differentiate
                    if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                break;

            case EnemyState.MOVING:
                // Move towards the current target
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // If we have reached the target tile, and we are chasing the player, update the path
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    if (state == EnemyState.CHASE)
                    {
                        // After reaching the target, recalculate the path towards a new target offset
                        path.Clear();

                        // Get a position 2 tiles away from the player (you can change this offset as needed)
                        Vector3 playerPosition = playerGameObject.transform.position;

                        // Offset in the X and Z directions (assuming 2 tiles away)
                        Vector3 offset = new Vector3(Random.Range(-2, 3), 0, Random.Range(-2, 3)); // Adjust the range as needed
                        Vector3 newTargetPosition = playerPosition + offset;

                        // Integrate FindClosestWalkableTile directly here
                        Tile closestTile = null;
                        float closestDistance = float.MaxValue;

                        // Iterate through all tiles and find the closest walkable one
                        foreach (Transform child in mapGenerator.transform)
                        {
                            Tile tile = child.GetComponent<Tile>();
                            if (tile != null && tile.mapTile.Walkable)
                            {
                                float distance = Vector3.Distance(newTargetPosition, tile.transform.position);
                                if (distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestTile = tile;
                                }
                            }
                        }

                        if (closestTile != null)
                        {
                            targetTile = closestTile;
                            path = pathFinder.RandomPath(currentTile, 20); // Use random path to simulate chasing

                            if (path.Count > 0)
                            {
                                targetTile = path.Dequeue();
                            }
                            else
                            {
                                state = EnemyState.DEFAULT; // No path to the new target, revert to default state
                            }
                        }
                        else
                        {
                            state = EnemyState.DEFAULT; // No valid target tile, revert to default state
                        }
                    }
                    else
                    {
                        state = EnemyState.DEFAULT; // Revert to default behavior when not chasing
                    }
                }
                break;

            case EnemyState.CHASE:
                // If the player is within vision range, we continue chasing
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    // The enemy chases a tile 2 tiles away from the player
                    Vector3 playerPosition = playerGameObject.transform.position;

                    // Offset in the X and Z directions (assuming 2 tiles away)
                    Vector3 offset = new Vector3(Random.Range(-2, 3), 0, Random.Range(-2, 3)); // Adjust the range as needed
                    Vector3 newTargetPosition = playerPosition + offset;

                    // Integrate FindClosestWalkableTile directly here
                    Tile closestTile = null;
                    float closestDistance = float.MaxValue;

                    // Iterate through all tiles and find the closest walkable one
                    foreach (Transform child in mapGenerator.transform)
                    {
                        Tile tile = child.GetComponent<Tile>();
                        if (tile != null && tile.mapTile.Walkable)
                        {
                            float distance = Vector3.Distance(newTargetPosition, tile.transform.position);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestTile = tile;
                            }
                        }
                    }

                    if (closestTile != null)
                    {
                        targetTile = closestTile;
                        path.Clear();
                        path = pathFinder.RandomPath(currentTile, 20);

                        if (path.Count > 0)
                        {
                            targetTile = path.Dequeue();
                            state = EnemyState.MOVING;
                        }
                    }
                    else
                    {
                        state = EnemyState.DEFAULT; // If no valid path found, revert to default state
                    }
                }
                else
                {
                    // If the player is no longer within vision range, revert to default state
                    state = EnemyState.DEFAULT;
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }
}
