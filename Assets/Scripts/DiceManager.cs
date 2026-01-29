using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    [SerializeField] private GameObject dicePrefab;
    private Rigidbody[] diceRigidbodies;

    [Header("Dicing Settings")]
    [SerializeField] private float diceForce;
    [SerializeField] private float diceTorque;

    // unity event to get result
    public UnityEvent<int> OnDiceResult;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject dice1 = Instantiate(dicePrefab, new Vector3(-2, 1, 0), Quaternion.identity);
        GameObject dice2 = Instantiate(dicePrefab, new Vector3(2, 1, 0), Quaternion.identity);
        diceRigidbodies = new Rigidbody[] { dice1.GetComponent<Rigidbody>(), dice2.GetComponent<Rigidbody>() };

        //StartCoroutine(StartTestRoll());

        OnDiceResult.AddListener(DebugResult);
    }

    private IEnumerator StartTestRoll()
    {
        yield return new WaitForSeconds(2f);
        RollDice();
        OnDiceResult.AddListener(DebugResult);
    }

    private void DebugResult(int result)
    {
        Debug.Log("Dice roll result: " + result);
        //OnDiceResult.RemoveListener(DebugResult);
        //StartCoroutine(StartTestRoll());
    }

    [ContextMenu("Roll Dice")]
    public void RollDice() {        
        int total = 0;
        foreach (Rigidbody diceRb in diceRigidbodies)
        {
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
            Vector3 forceDirection = new Vector3(Random.Range(-1f, 1f), 1, Random.Range(-1f, 1f)).normalized;
            diceRb.AddForce(forceDirection * diceForce, ForceMode.Impulse);
            diceRb.AddTorque(Random.insideUnitSphere * diceTorque, ForceMode.Impulse);
        }
        StartCoroutine(WaitForDiceAndGetResult());
    }

    private IEnumerator WaitForDiceAndGetResult()
    {
        bool allDiceStopped = false;
        while (!allDiceStopped)
        {
            yield return new WaitForSeconds(1f);

            if (diceRigidbodies != null)
            {
                allDiceStopped = true;
                foreach (Rigidbody diceRb in diceRigidbodies)
                {
                    if (diceRb.linearVelocity.magnitude > 0.1f || diceRb.angularVelocity.magnitude > 0.1f)
                    {
                        allDiceStopped = false;
                        break;
                    }
                }

            }

        }
        int total = 0;
        Transform[] faces = null;
        foreach (Rigidbody diceRb in diceRigidbodies)
        {
            faces = diceRb.gameObject.GetComponentsInChildren<Transform>();

            // sort faces by distance to ground; highest face will be at index 0 after sorting
            System.Array.Sort(faces, (a, b) => a.position.y.CompareTo(b.position.y));
            string n = faces[faces.Length - 1].gameObject.name;
            int diceValue = int.Parse(n.Substring(n.Length-1));
            
            total += diceValue;
        }
        OnDiceResult.Invoke(total);



    }

}
