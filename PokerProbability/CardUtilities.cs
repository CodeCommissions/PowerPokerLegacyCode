using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokerProbability
{
    public static class CardUtilities
    {
        private static string[] CARD_NAMES = { "High Card", "Pair", "Jacks or Better", "Two Pair", "Three of a Kind", "Straight", "Flush", "Full House", "Four of a Kind", "Straight Flush", "Royal Flush", };
        

        /// <summary>
        /// We use this method with the inbuilt list sort function to order cards based on their face value rather than their integer value 
        /// Integer values goes from 0-51, face values go from 2-A (or 0-12))
        /// And incomplete hands contain '-1's which are always moved to the end of the hand.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int NumberCardComparer(int a, int b)
        {
            //We left these here as a reminder of the order we follow: sort by value, then if tied, by suit (suit is just for consistency)
            //string[] value = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            //string[] suites = { "♥", "♣", "♦", "♠" };

            //return value[cardNum % 13] + suites[cardNum / 13];

            if (a == b)
                return 0;

            if (b == -1)
                return -1;

            if (a > 51 || b > 51)
                return 1;
            if (a < 0 || b < 0)
                return -1;

            //number matters more than suite
            if (a % 13 > b % 13)
                return 1;
            if (a % 13 < b % 13)
                return -1;
            //getting here means that they must have at least the same non-suite value

            if (a / 13 > b / 13)
                return 1;
            if (a / 13 < b / 13)
                return -1;

            //You should only be able to get here if is more than one deck in play (so never if your playing normal poker)
            return 0;
        }

        /// <summary>
        /// Takes a hand, calculates it's numeric values, and then converts that into the name of the hand. (Basic names only, i.e. we don't say Ace-High-Straight)
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static string getSortedHandName(List<int> cards)
        {
            return getHandValueName(getSortedHandValue(cards));
        }

        public static string getHandName(List<int> cards)
        {
            cards.Sort(NumberCardComparer);

            return getHandValueName(getSortedHandValue(cards));
        }

        public static string getHandValueName(double handValue)
        {
            return CARD_NAMES[(int)Math.Floor(handValue)];
        }

        public static double getHandValue(List<int> cards)
        {
            cards.Sort(NumberCardComparer);
            return getSortedHandValue(cards);
        }

        public static IEnumerable<Tuple<int[], int[], int[], int[]>> enumerateHandMasks_ThreadVersion()
        {
            for (int i1 = 0; i1 < 2; i1++)
                for (int i2 = 0; i2 < 2; i2++)
                    for (int i3 = 0; i3 < 2; i3++)
                        yield return new Tuple<int[], int[], int[], int[]>(
                                (new int[] { i1, i2, i3, 0, 0 }),
                                (new int[] { i1, i2, i3, 0, 1 }),
                                (new int[] { i1, i2, i3, 1, 0 }),
                                (new int[] { i1, i2, i3, 1, 1 }));
        }
        
      

        /// <summary>
        /// This method takes a set of 5 cards in an array, and return a value between 0 and 9(inclusive) that represents it value on the 'poker hand scale'.
        /// Each value when truncated results in an integer from 0-9 that corresponds to the name of a hand type (eg 0-> high card, 9 -> royal flush).
        /// We have taken special care to ensure that even when ties occur the hand types never overlap in value.
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static double getSortedHandValue(List<int> cards)
        {
            //I realise that this method is fairly long, however I feel that arbirarily restricting the length of a method is often senseless.
            //For example this particular method could be shorted by moving each of the checks to their own methods...but if those methods aren't going to be used elseware then doesn't it just complicate navigation?
            //If I were feeling a little crazy this entire this could be chrunched into just one return statements, it would look a bit like this (where ... is a code gap):
            //return (cards[0] % 13 + 1 == cards[1] % 13 .... ? 8 + (cards[4] % 13)/13.0 : (cards[0] % 13 .... cards[1] % 13 ? 7 + (cards[2] % 13)/13.0) : (...);
            //One line...but not readable, maintainable or testable

            //order as most valuable followed by less valueable

            //In the event of a tie we follow assorted tiw braking rules and add the result to the the value we have so far
            //We diminish the important of cards as we go on (division by 13 ensures than there is never an overlap which would occur if dividing by 10 on a card that modded down to 10,11 or 12)

            #region straight flush (and royal when the high card is an ace)
            if ((cards[0] % 13 + 1 == cards[1] % 13 && cards[1] % 13 + 1 == cards[2] % 13 && cards[2] % 13 + 1 == cards[3] % 13 && cards[3] % 13 + 1 == cards[4] % 13 || //a non-circular sequence
                 cards[0] % 13 == 0 && cards[1] % 13 == 1 && cards[2] % 13 == 2 && cards[3] % 13 == 3 && cards[4] % 13 == 12) && //a special case when ace is treated as a 1
                 cards[0] / 13 == cards[1] / 13 && cards[1] / 13 == cards[2] / 13 && cards[2] / 13 == cards[3] / 13 && cards[3] / 13 == cards[4] / 13)//all the same suite
            {
                //return the base value, with an offset for the highest card. Suites are not counted towards values, and thus are not used to break ties

                if (cards[3] % 13 == 3 && cards[4] % 13 == 12) //catering for A2345
                    return 9 + (cards[3] % 13) / 13.0; //The highest card in a 5-high straight is the five (index 3) not the Ace (index 4)

                if (cards[4] % 13 == 12) //an ace high straight flush is a royal flush 
                    return 10;

                return 9 + (cards[4] % 13) / 13.0;
            }
            #endregion

            //four of a kind
            if (cards[0] % 13 == cards[1] % 13 && cards[1] % 13 == cards[2] % 13 && cards[2] % 13 == cards[3] % 13 ||
                cards[1] % 13 == cards[2] % 13 && cards[2] % 13 == cards[3] % 13 && cards[3] % 13 == cards[4] % 13)
            {
                //suite never come into the equation for 4 of a kind, becuase you can only every have one set of a particular number
                //what does matter is the value of the 4-cards
                return 8 + (cards[2] % 13) / 13.0;

            }

            //full house
            if (cards[0] % 13 == cards[1] % 13 && cards[1] % 13 == cards[2] % 13 && /*first three are equal*/ cards[3] % 13 == cards[4] % 13 /* and the last 2*/
               || cards[0] % 13 == cards[1] % 13 /*first three are equal*/ && cards[2] % 13 == cards[3] % 13 && cards[3] % 13 == cards[4] % 13)
            {
                return 7 + (cards[2] % 13) / 13.0;//the centre card (again) always belongs inside the triple. And is used as a relative gauge for when comparing triples
            }

            //at this point we KNOW that non of the higher valued outputs have passed so we can safely ignore cases that WOULD make them pass (eg a straight flush is a special straight, but not the other way around)
            //flush
            if (cards[0] / 13 == cards[1] / 13 && cards[1] / 13 == cards[2] / 13 && cards[2] / 13 == cards[3] / 13 && cards[3] / 13 == cards[4] / 13)//all the same suite
            {
                //a flush tie is broken by the highest card (always in position 4)
                return 6 + (cards[4] % 13) / 13.0;
            }

            //straight
            if (cards[0] % 13 + 1 == cards[1] % 13 && cards[1] % 13 + 1 == cards[2] % 13 && cards[2] % 13 + 1 == cards[3] % 13 && cards[3] % 13 + 1 == cards[4] % 13 || //a non-circular sequence
                 cards[0] % 13 == 0 && cards[1] % 13 == 1 && cards[2] % 13 == 2 && cards[3] % 13 == 3 && cards[4] % 13 == 12) //a special case when ace is treated as a 1
            {
                if (cards[3] % 13 == 3 && cards[4] % 13 == 12) //catering for A2345
                    return 5 + (cards[3] % 13) / 13.0; //The highest card in a 5-high straight is the five (index 3) not the Ace (index 4)
                else
                    return 5 + (cards[4] % 13) / 13.0;
            }

            //with three of a kind the centre card always overlaps one of the three positble locations for the triple set
            //three of a kind 
            if (cards[0] % 13 == cards[1] % 13 && cards[1] % 13 == cards[2] % 13 || //left aligned triple
                cards[1] % 13 == cards[2] % 13 && cards[2] % 13 == cards[3] % 13 ||
                cards[2] % 13 == cards[3] % 13 && cards[3] % 13 == cards[4] % 13) //right allighen
            {
                return 4 + (cards[2] % 13) / 13.0;
            }

            //Three arrangments for a 2 pair -> [0,1],[2,3][4] -- [0,1][2][3,4] -- [0][1,2][3,4]
            //however this is the first time when ties need to be broken...and they are broken in two ways: first by looking at the second most valueable pair...second by looking at the value of the final card
            //becuase of this tie breaking we need to know which of the three cases it is so we can pick out the tie breaking card (without a tie the winner will be determined by whatever is at index 4, and the low pair is always at index 1)
            //two pair
            if (cards[0] % 13 == cards[1] % 13 && cards[2] % 13 == cards[3] % 13) //[0,1],[2,3][4] 
            {
                //index 4 is the odd one out.
                //           top pair value +     low pair value                + odd card value
                return 3 + (cards[3] % 13) / 13.0 + (cards[1] % 13) / Math.Pow(13, 2) + (cards[4] % 13) / Math.Pow(13, 3);
            }
            if (cards[0] % 13 == cards[1] % 13 && cards[3] % 13 == cards[4] % 13) //[0,1][2][3,4]
            {
                //index 2 is the odd one out
                return 3 + (cards[3] % 13) / 13.0 + (cards[1] % 13) / Math.Pow(13, 2) + (cards[2] % 13) / Math.Pow(13, 3);
            }
            if (cards[1] % 13 == cards[2] % 13.0 && cards[3] % 13 == cards[4] % 13) //[0][1,2][3,4]
            {
                //index 0 is the odd one out
                return 3 + (cards[3] % 13) / 13.0 + (cards[1] % 13) / Math.Pow(13, 2) + (cards[0] % 13) / Math.Pow(13, 3);
            }

            #region both types of pair hands
            //there are three pair arrangments - [0,1],2,3,4 -- 0,[1,2],3,4 -- 0,1,[2,3],4 -- 0,1,2,[3,4]
            //Unlike the two pairs there are no patterns between the 4 combintations at all (not that the patterns for 2pairs did much good, seeing as we needed to isolate the lone card every time
            //single pair
            if (cards[0] % 13 == cards[1] % 13)//[0,1],2,3,4
            {
                //are the pair cards greater than 10 (i.e. jacks or better)
                if(cards[0] % 13 > 8)
                    return 2 + (cards[0] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[2] % 13) / Math.Pow(13, 4); //we need to include diminishing tie breaker values for all non-pair cards
          
                //'Low pair'
                return 1 + (cards[0] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[2] % 13) / Math.Pow(13, 4); //we need to include diminishing tie breaker values for all non-pair cards
            }
            if (cards[1] % 13 == cards[2] % 13)//0,[1,2],3,4
            {
                //are the pair cards greater than 10 (i.e. jacks or better)
                if (cards[1] % 13 > 8)
                    return 2 + (cards[0] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[2] % 13) / Math.Pow(13, 4); //we need to include diminishing tie breaker values for all non-pair cards

                //'Low pair'
                return 1 + (cards[1] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[0] % 13) / Math.Pow(13, 4);
            }
            if (cards[2] % 13 == cards[3] % 13)//0,1,[2,3],4
            {
                //are the pair cards greater than 10 (i.e. jacks or better)
                if (cards[2] % 13 > 8)
                    return 2 + (cards[0] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[2] % 13) / Math.Pow(13, 4); //we need to include diminishing tie breaker values for all non-pair cards

                //'Low pair'
                return 1 + (cards[2] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[1] % 13) / Math.Pow(13, 3) + (cards[0] % 13) / Math.Pow(13, 4);
            }
            if (cards[3] % 13 == cards[4] % 13)//0,1,2,[3,4]
            {
                //are the pair cards greater than 10 (i.e. jacks or better)
                if (cards[3] % 13 > 8)
                    return 2 + (cards[0] % 13) / 13.0 +
                            (cards[4] % 13) / Math.Pow(13, 2) + (cards[3] % 13) / Math.Pow(13, 3) + (cards[2] % 13) / Math.Pow(13, 4); //we need to include diminishing tie breaker values for all non-pair cards

                //'Low pair'
                return 1 + (cards[3] % 13) / 13 +
                            (cards[2] % 13) / Math.Pow(13, 2) + (cards[1] % 13) / Math.Pow(13, 3) + (cards[0] % 13) / Math.Pow(13, 4);
            }
            #endregion

            //it's a sad state when a winner comes down to comparing high card values :P
            //High Card
            return 0 + (cards[4] % 13) / 13.0 +
                            (cards[3] % 13) / Math.Pow(13, 2) +
                                (cards[2] % 13) / Math.Pow(13, 3) +
                                    (cards[1] % 13) / Math.Pow(13, 4) +
                                        (cards[0] % 13) / Math.Pow(13, 5);
        }

        /// <summary>
        /// Takes an integer card from 0 to 51 and returns in 2-3 char string representation
        /// </summary>
        /// <param name="cardNum"></param>
        /// <returns></returns>
        public static string convertToStringCard(int cardNum)
        {
            if (cardNum < 0 || cardNum > 51)
                return "";
            string[] value = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            string[] suites = { "♥", "♣", "♦", "♠" };

            return value[cardNum % 13] + suites[cardNum / 13];
        }

        /// <summary>
        /// Takes the string represetnation of a card and returns its integer equivalent
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public static int convertFromStringCard(string card)
        {
            if (card == "")
                return -1;

            List<string> value = new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            List<string> suites = new List<string> { "♥", "♣", "♦", "♠" };
            if (card.Contains("10"))
            {
                return 8 + suites.IndexOf(card[2].ToString()) * 13;
            }
            else
                return value.IndexOf(card[0].ToString()) + suites.IndexOf(card[1].ToString()) * 13;
        }

        /// <summary>
        /// This method sorts the clone of the deck based on the Face value rather than the integer value of the cards.
        /// This means that the decks clones order (which always starts as something like 1..2...5...14..33..51) will altered to 
        /// Go from least valuable card (2) to most valuable card (A). This is usefull as if one wants to perform multiple
        /// operations based on variable sorted hands it odoesn't make sense to have to sort each hand individually.
        /// In short, this is a method for effiency of operation.
        /// </summary>
        /// <returns>Returns an integer valued list</returns>
        public static List<int> createSortedCloneOfDeck(List<string> deckContent)
        {
            var clone = new List<int>();

            //populate new list with the converted string cards
            foreach (var stringCard in deckContent)
                clone.Add(CardUtilities.convertFromStringCard(stringCard));

            clone.Sort(CardUtilities.NumberCardComparer);
            return clone;
        }

        /// <summary>
        /// the goal behind this recursive fucntion is to enumerate all the possible outputs of a sequence of numbers
        /// more specifically the output from a length 1 test with boundaries between 0 and 10, will output 10 numbers
        /// while a length 2 output with range 0-10 will give back [[1,2],[1,3]...[1,9][2,3]...[8,9]].
        /// For a poker hand where cards are represented as numbers you would want length 5, lowerbound 0 upperbound 52. (provided it's a full deck)
        /// </summary>
        /// <param name="lowerbound"></param>
        /// <param name="upperbound">The non-inclusive upper bound</param>
        /// <param name="desiredLength">The desired number of elements by the end</param>
        /// <param name="predecessorsin">The list of numbers so far</param>
        /// <returns></returns>
        public static IEnumerable<List<int>> enumerateUniqueIntegerCombinations(int lowerbound, int upperbound, int desiredLength, List<int> predecessorsin = null)
        {
            List<int> predecessors;
            //we want to clone the array so that every yield gives up a unique output
            if (predecessorsin == null)
                predecessors = new List<int>();
            else
                predecessors = new List<int>(predecessorsin);


            //if we've reached the length we wanted, then stop
            if (predecessors.Count < desiredLength)
            {
                predecessors.Add(-1); //this number is arbitary and will be changed inside this loop

                //we have to start i off at different sizes based on whether we just created the list or if were already elements in it (prevents index exceptions)
                for (int i = predecessors.Count == 1 ? lowerbound : predecessors[predecessors.Count - 2] + 1; i < upperbound; i++)
                {
                    predecessors[predecessors.Count - 1] = i;

                    //Let the recursion begin! Here we basically check if we have reached our desired state and if so we start tracking back up the stack. Otherwise we recurse again for every possible remaining combo
                    if (predecessors.Count == desiredLength)
                        yield return predecessors;
                    else
                        foreach (var x in enumerateUniqueIntegerCombinations(lowerbound, upperbound, desiredLength, predecessors))
                            yield return x;
                }
            }
        }

        /// <summary>
        /// Returns each and every possible hand that can be drawn from a given deck
        /// </summary>
        /// <param name="currentDeck"></param>
        /// <returns></returns>
        public static IEnumerable<string> enumeratePossibleHands(List<int> currentDeck)
        {
            foreach (List<int> indexCombo in enumerateUniqueIntegerCombinations(0, currentDeck.Count, 5))
            {
                yield return getHandName(new List<int>() { currentDeck[indexCombo[0]], currentDeck[indexCombo[1]], currentDeck[indexCombo[2]], currentDeck[indexCombo[3]], currentDeck[indexCombo[4]] });
            }
        }

        /// <summary>
        /// Returns each and every possible hand for a given discard mask-hand combo
        /// </summary>
        /// <param name="originalHand">The currently held hand that you *might* discard from</param>
        /// <param name="currentDeck">The rest of the deck (it really shouldn't contain any of the cards from originalHand</param>
        /// <param name="drawMask">1 means hold corresponding card, 0 means discard it</param>
        /// <returns></returns>
        public static IEnumerable<string> enumeratePossibleHands(List<int> originalHand, List<int> currentDeck, int[] drawMask)
        {
            //If the current mask is all zeros then the original hand doesn't matter, and we can increase efficiency by skipping all the list merging that would have to take place otherwise
            if (drawMask[0] == 0 && drawMask[1] == 0 && drawMask[2] == 0 && drawMask[3] == 0 && drawMask[4] == 0)
            {
                foreach (string val in enumeratePossibleHands(currentDeck))
                    yield return val;
                yield break;
            }

            //create the base hand that will be refered to later for each new combo
            int[] newHand = new int[]{-1,-1,-1,-1,-1};
            int cnt = 0;
            for (int i = 0; i < drawMask.Length; i++)
            {
                if(drawMask[i] == 1)
                {
                    newHand[cnt] = originalHand[i];
                    cnt++;
                }
            }

            //by this stage we have a base hand, and we know its element count (i.e. where the '-1's begin)

            //iterate over every combination of indexes in the deck
            foreach (List<int> indexCombo in enumerateUniqueIntegerCombinations(0, currentDeck.Count, 5 - cnt))
            {
                //indexCombo and newhand are always sorted...we can use that to improve hand creation efficiency
                List<int> tempHand = new List<int>(5);
                
                //insert elements from both collections,
                int i1 = 0, i2 = 0;
                while(i1 + i2 < 5)
                {
                    //Didnt use the card comparator function as it induces extra function call overhead.....might be interesting to pit the two comparion technciques against each other to see exaclty how large the difference would be

                    //Once we run out of new combo cards, there can be only ones from the original hand left
                    if (i2 >= indexCombo.Count)
                    {
                        tempHand.Add(newHand[i1++]);
                        continue;
                    }

                        //(-1) checks if we've run out of original cards, in which case short circuit evluation ensures that erroneous negative mods are avoided
                        //the second half incorporates face and suit value calcuation into one large equation (this avoid having 4 ifs, which *may* be less efficient)
                    if (newHand[i1] == -1 || newHand[i1] % 13 + (newHand[i1] / 13)/5 > currentDeck[indexCombo[i2]] % 13 + (currentDeck[indexCombo[i2]] / 13)/5)
                        tempHand.Add(currentDeck[indexCombo[i2++]]);
                    else
                        tempHand.Add(newHand[i1++]);
                }

                yield return getHandName(tempHand);
            }

        }
    }
}
