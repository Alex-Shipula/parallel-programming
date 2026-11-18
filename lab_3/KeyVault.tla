---- MODULE KeyVault ----
EXTENDS Naturals, FiniteSets

CONSTANTS Procs, MaxOps
VARIABLES pc, want, turn, vault_holder, ops_left

vars == <<pc, want, turn, vault_holder, ops_left>>

TypeOK ==
  /\ pc \in [Procs -> {"idle", "set_turn", "wait", "cs", "closing"}]
  /\ want \in [Procs -> BOOLEAN]
  /\ turn \in Procs
  /\ vault_holder \in Procs \cup {"none"}
  /\ ops_left \in [Procs -> 0..MaxOps]

Init ==
  /\ pc = [p \in Procs |-> "idle"]
  /\ want = [p \in Procs |-> FALSE]
  /\ turn \in Procs
  /\ vault_holder = "none"
  /\ ops_left = [p \in Procs |-> 0]

====