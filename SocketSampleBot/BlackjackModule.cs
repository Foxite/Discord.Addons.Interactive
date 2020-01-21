using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace SampleBot {
	public class BlackjackModule : InteractiveBase<SocketCommandContext> {
		[Command("play blackjack", RunMode = RunMode.Async)]
		public async Task PlayBlackjack(params IUser[] users) {
			if (users == null) {
				await ReplyAsync("Er moet minstens 1 andere speler zijn voor Blackjack.");
				return;
			}

			await ReplyAsync("At least one player needs to accept the invitation to join. They have 60 seconds to do so.");

			string[] positive = new[] { "yes", "join", "ok", "accept" };
			string[] negative = new[] { "no", "ignore", "deny", "refuse" };

			var joinedUsers = new HashSet<IUser>();
			await foreach (SocketMessage msg in WaitAllPlayers(users, new Criteria<SocketMessage>(
					new EnsureSourceUserCriterion(),
					new EnsureSourceChannelCriterion(),
					new EnsureOneOfCriterion(new[] { positive, negative }.SelectMany(s => s))
				))) {
				if (positive.Contains(msg.Content.ToLower())) {
					joinedUsers.Add(msg.Author);
					await ReplyAsync("Confirmed: " + ((msg.Author as IGuildUser)?.Nickname ?? msg.Author.Username) + " is in the game.");
				} else if (negative.Contains(msg.Content.ToLower())) {
					await ReplyAsync("Confirmed: " + ((msg.Author as IGuildUser)?.Nickname ?? msg.Author.Username) + " is not playing.");
				}
			}
			if (joinedUsers.Count == 0) {
				await ReplyAsync("Can't play the game because nobody has accepted the invitation.");
			} else {
				await PlayBlackjack(joinedUsers);
			}
		}

		private async IAsyncEnumerable<SocketMessage> WaitAllPlayers(IEnumerable<IUser> users, ICriterion<SocketMessage> criterion) {
			var usersRemaining = new HashSet<IUser>(users);
			SocketMessage result;
			while (usersRemaining.Count > 0 && (result = await NextMessageAsync(criterion, TimeSpan.FromSeconds(60))) != null) {
				yield return result;
			}
		}

		private async Task PlayBlackjack(IEnumerable<IUser> users) {

		}
	}
}
