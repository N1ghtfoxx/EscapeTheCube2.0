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
    [SerializeField] private float waitTimeBeforeResult;
    [SerializeField] private Transform throwPosition;

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
        GameObject dice1 = Instantiate(dicePrefab, throwPosition.position, Quaternion.identity);
        GameObject dice2 = Instantiate(dicePrefab, throwPosition.position, Quaternion.identity);
        diceRigidbodies = new Rigidbody[] { dice1.GetComponent<Rigidbody>(), dice2.GetComponent<Rigidbody>() };

        for (int i = 0; i < diceRigidbodies.Length; i++)
        {
            diceRigidbodies[i].isKinematic = true;
        }

        OnDiceResult.AddListener(DebugResult);
    }

    private void DebugResult(int result)
    {
        Debug.Log("Dice roll result: " + result);
    }

    [ContextMenu("Roll Dice")]
    public void RollDice() {        
        foreach (Rigidbody diceRb in diceRigidbodies)
        {
            diceRb.isKinematic = false;
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
            diceRb.transform.position = new Vector3(throwPosition.position.x, throwPosition.position.y, throwPosition.position.z + Random.Range(-3.5f, 3.5f));
            diceRb.transform.rotation = Random.rotation;
            Vector3 forceDirection = new Vector3(Random.Range(-3f, -1f), 1f, Random.Range(-1f, 1f)).normalized;
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
            diceRb.isKinematic = true;
        }
        yield return new WaitForSeconds(waitTimeBeforeResult);
        OnDiceResult.Invoke(total);
        foreach (Rigidbody diceRb in diceRigidbodies)
        {
            diceRb.transform.position = throwPosition.position;
        }

    }

}
