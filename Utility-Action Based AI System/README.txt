This is a sample from the selfmade AI System currently in use for a personnal Action RPG / Roguelite Multiplayer Dungeon Crawler game.

The system is mainly based on the utility theory and on an 'action' logic.

Each AI Action is a child class of GenericAiAction and implement 3 main methods
	- ComputePriority : the priority is used as a weight for a weighted random selection on each think loop of the brain.
	- Execute : the actual execution of the action
	- Check Interruption : a boolean method that should return true if the action should interrupt herself (for example, a GoToTarget action will interrupt if the target is dead.)

When an action is running, the brain normally doesn't evaluate the other actions (depends of the running action).
When an action has finished, it notifies the brain as he can reevalute actions.
An action currently running can interrupt itself.
Parameters allow an action to be overidden by another one if a threshold is achieved.

Each time the thinkTimer will reach the desired time, the brain evaluate the running action interruption condition.
If no running action, the brain evaluates the list of action and selects one randomly based on the priority, to add more "human" feel on the decisions, are they are not always the 'best' for the situation.



