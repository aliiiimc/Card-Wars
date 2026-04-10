using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")] // This attribute allows us to 
// create an instance of this ScriptableObject from the Unity Editor
public class GameConfig : ScriptableObject
{
    public int startingMoney = 3;
    public int startingFortHp = 20;
    public int startingHandSize = 5;
    public int maxHandSize = 7;
    public int moneyPerTurn = 2;
    public int buyCost = 1;

}
