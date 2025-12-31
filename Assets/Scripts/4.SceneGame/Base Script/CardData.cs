using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Value
{
    NoneValue = 0,
    Ace,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Page,
    Knight,
    Queen,
    King
};

public enum Type
{
    NoneType,
    Swords,
    Cups,
    Wands,
    Pentacles
};

public enum Set
{
    NoneSet,
    Duo,
    Triplet,
    Quartet,
    Line,
    Line4,
    Straight,
    RoyalStraight,
    League,
    Link,
};

public enum Place
{
    None,
    inDeck,
    inHand,
    inDiscard,
    inSpread,
    inCall,
    inSnap
}


[System.Serializable]
public class CardData
{
    public string name;
    public Value value;
    public Type type;
}

// 0. Ace of Swords
// 1. Two of Swords
// 2. Three of Swords
// 3. Four of Swords
// 4. Five of Swords
// 5. Six of Swords
// 6. Seven of Swords
// 7. Eight of Swords
// 8. Nine of Swords
// 9. Ten of Swords
// 10. Page of Swords
// 11. Knight of Swords
// 12. Queen of Swords
// 13. King of Swords
// 14. Ace of Cups
// 15. Two of Cups
// 16. Three of Cups
// 17. Four of Cups
// 18. Five of Cups
// 19. Six of Cups
// 20. Seven of Cups
// 21. Eight of Cups
// 22. Nine of Cups
// 23. Ten of Cups
// 24. Page of Cups
// 25. Knight of Cups
// 26. Queen of Cups
// 27. King of Cups
// 28. Ace of Wands
// 29. Two of Wands
// 30. Three of Wands
// 31. Four of Wands
// 32. Five of Wands
// 33. Six of Wands
// 34. Seven of Wands
// 35. Eight of Wands
// 36. Nine of Wands
// 37. Ten of Wands
// 38. Page of Wands
// 39. Knight of Wands
// 40. Queen of Wands
// 41. King of Wands
// 42. Ace of Pentacles
// 43. Two of Pentacles
// 44. Three of Pentacles
// 45. Four of Pentacles
// 46. Five of Pentacles
// 47. Six of Pentacles
// 48. Seven of Pentacles
// 49. Eight of Pentacles
// 50. Nine of Pentacles
// 51. Ten of Pentacles
// 52. Page of Pentacles
// 53. Knight of Pentacles
// 54. Queen of Pentacles
// 55. King of Pentacles