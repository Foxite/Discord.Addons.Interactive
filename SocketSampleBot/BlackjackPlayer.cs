using System.Collections.Generic;
using System.Linq;
using Discord;

namespace SampleBot {
	public class BlackjackPlayer {
		public (Card, Card) PrivateCards { get; }
		public List<Card> PublicCards { get; }
		public IUser DiscordUser { get; }
		public int ChipCount { get; set; }
		public int Value {
			get {
				int value = 0;
				int aces = 0;
				foreach (Card card in PublicCards.Concat(new[] { PrivateCards.Item1, PrivateCards.Item2 })) {
					if (card == Card.Ace) {
						aces++;
					} else {
						if (card == Card.Jack || card == Card.Queen || card == Card.King) {
							value += 10;
						} else {
							value += (int) card;
						}
					}
				}
				
				// TODO if you have 4 aces and a base value of 10, we should count them as 4 rather than 13
				for (int i = aces - 1; i >= 0; i--) {
					if (value < 11) {
						value += 11;
					} else {
						value++;
					}
				}
				return value;
			}
		}

		public BlackjackPlayer(IUser user, Card privateCard1, Card privateCard2) {
			DiscordUser = user;
			PrivateCards = (privateCard1, privateCard2);
			PublicCards = new List<Card>();
		}
	}
}
