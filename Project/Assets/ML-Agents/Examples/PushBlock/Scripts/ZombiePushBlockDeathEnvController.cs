using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Extensions.Teams;
using UnityEngine;

public class ZombiePushBlockDeathEnvController : MonoBehaviour
{
    [System.Serializable]
    public class AgentInfo
    {
        public PushAgentCollab Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    [System.Serializable]
    public class ZombieInfo
    {
        public SimpleNPC Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    [System.Serializable]
    public class BlockInfo
    {
        public Transform T;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    public GameObject area;

    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    public List<AgentInfo> AgentsList = new List<AgentInfo>();
    public List<ZombieInfo> ZombiesList = new List<ZombieInfo>();
    public List<BlockInfo> BlocksList = new List<BlockInfo>();

    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    public bool UseRandomBlockRotation = true;
    public bool UseRandomBlockPosition = true;
    public bool UseTeamManager = true;
    public bool UseTeamReward = true;
    PushBlockSettings m_PushBlockSettings;

    private int m_NumberOfRemainingBlocks;
    private BaseTeamManager m_TeamManager;

    void Start()
    {

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        foreach (var item in BlocksList)
        {
            item.StartingPos = item.T.transform.position;
            item.StartingRot = item.T.transform.rotation;
            item.Rb = item.T.GetComponent<Rigidbody>();
        }
        // Initialize TeamManager
        if (UseTeamManager)
        {
            if (UseTeamReward)
            {
                print("create PushBlockTeamManager");
                m_TeamManager = new PushBlockTeamManager();
            }
            else
            {
                print("create BaseTeamManager");
                m_TeamManager = new BaseTeamManager();
            }
        }

        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
            // Add to team manager
            item.Agent.SetTeamManager(m_TeamManager);
        }
        foreach (var item in ZombiesList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Col = item.Agent.GetComponent<Collider>();
        }

        ResetScene();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            ResetScene();
        }
    }

    //Kill/disable an agent
    public void KillAgent(Collision col, Transform t)
    {
        // print($"Zombie {t.gameObject.GetInstanceID()} ate Agent {col.gameObject.GetInstanceID()}");

        //Disable killed Agent
        foreach (var item in AgentsList)
        {
            if (item.Col == col.collider)
            {
                print($"call endepisode on agent {item.Agent.gameObject.GetInstanceID()}");
                item.Agent.EndEpisode();
                item.Col.gameObject.SetActive(false);
                break;
            }
        }

        //End Episode
        foreach (var item in ZombiesList)
        {
            if (item.Agent.transform == t)
            {
                item.Agent.gameObject.SetActive(false);
                break;
            }
        }
    }


    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock(BlockInfo block)
    {
        // Get a random position for the block.
        block.T.position = GetRandomSpawnPos();

        // Reset block velocity back to zero.
        block.Rb.velocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        block.Rb.angularVelocity = Vector3.zero;
    }


    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Called when the agent moves the block into the goal.
    /// </summary>
    public void ScoredAGoal(Collider col, float score)
    {
        // //Decrement the counter
        // m_NumberOfRemainingBlocks--;
        //
        // //Are we done?
        // bool done = m_NumberOfRemainingBlocks == 0;
        //
        // //Disable the block
        // col.gameObject.SetActive(false);

        //Give Agent Rewards
        print($"scored");
        if (UseTeamManager && UseTeamReward)
        {
            var pushManager = (PushBlockTeamManager)m_TeamManager;
            pushManager.AddTeamReward(score);
        }
        else
        {
            foreach (var item in AgentsList)
            {
                if (item.Agent.gameObject.activeInHierarchy)
                {
                    // print($"{item.Agent.name} scored");
                    item.Agent.AddReward(score);
                }
            }
        }

        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));

        // if (done)
        // {
        //Reset assets
        ResetScene();
        // }
    }

    public void ZombieTouchedBlock()
    {
        print("Zombie Touched Block");
        //Give Agent Rewards
        if (UseTeamManager && UseTeamReward)
        {
            var pushManager = (PushBlockTeamManager)m_TeamManager;
            pushManager.AddTeamReward(-1);
        }
        else
        {
            foreach (var item in AgentsList)
            {
                item.Agent.AddReward(-1);
            }
        }

        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.failMaterial, 0.5f));
        ResetScene();

    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    void ResetScene()
    {
        print("Reset Scene");
        m_ResetTimer = 0;

        //Random platform rot
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        //End Episode
        foreach (var item in AgentsList)
        {
            if (!item.Agent)
            {
                return;
            }

            // not disabling agent here might cause problem with the done state
            // item.Agent.EndEpisode();
            item.Agent.gameObject.SetActive(false);
        }

        // OnTeamDone has to be called after agents has called EndEpisode or been disabled.
        if (UseTeamManager && UseTeamReward)
        {
            var pushManager = (PushBlockTeamManager)m_TeamManager;
            pushManager.OnTeamDone();
        }

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.gameObject.SetActive(true);
        }

        //Reset Blocks
        foreach (var item in BlocksList)
        {
            var pos = UseRandomBlockPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomBlockRotation ? GetRandomRot() : item.StartingRot;

            item.T.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.T.gameObject.SetActive(true);
        }
        //End Episode
        foreach (var item in ZombiesList)
        {
            if (!item.Agent)
            {
                return;
            }
            // item.Agent.EndEpisode();
            item.Agent.transform.SetPositionAndRotation(item.StartingPos, item.StartingRot);
            item.Agent.SetRandomWalkSpeed();
            item.Agent.gameObject.SetActive(true);
        }

        //Reset counter
        m_NumberOfRemainingBlocks = BlocksList.Count;
        // m_NumberOfRemainingBlocks = 2;
        print("EndEpisode");
    }
}
