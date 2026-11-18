---- MODULE KeyVault ----
EXTENDS Naturals, FiniteSets

CONSTANTS Procs, MaxOps
VARIABLES pc, want, turn, vault_holder, ops_left

vars == <<pc, want, turn, vault_holder, ops_left>>

Other(p) == CHOOSE q \in Procs : q # p

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

RequestAccess(p) ==
  /\ pc[p] = "idle"
  /\ want' = [want EXCEPT ![p] = TRUE]
  /\ pc' = [pc EXCEPT ![p] = "set_turn"]
  /\ UNCHANGED <<turn, vault_holder, ops_left>>

PassTurn(p) ==
  /\ pc[p] = "set_turn"
  /\ turn' = Other(p)
  /\ pc' = [pc EXCEPT ![p] = "wait"]
  /\ UNCHANGED <<want, vault_holder, ops_left>>

EnterVault(p) ==
  /\ pc[p] = "wait"
  /\ (\A q \in Procs \ {p} : ~want[q]) \/ turn = p
  /\ pc' = [pc EXCEPT ![p] = "cs"]
  /\ vault_holder' = p
  /\ ops_left' = [ops_left EXCEPT ![p] = MaxOps]
  /\ UNCHANGED <<want, turn>>

ExecuteOp(p) ==
  /\ pc[p] = "cs"
  /\ ops_left[p] > 0
  /\ ops_left' = [ops_left EXCEPT ![p] = ops_left[p] - 1]
  /\ UNCHANGED <<pc, want, turn, vault_holder>>

FinishWork(p) ==
  /\ pc[p] = "cs"
  /\ ops_left[p] = 0
  /\ pc' = [pc EXCEPT ![p] = "closing"]
  /\ vault_holder' = "none"
  /\ UNCHANGED <<want, turn, ops_left>>

ReleaseAccess(p) ==
  /\ pc[p] = "closing"
  /\ want' = [want EXCEPT ![p] = FALSE]
  /\ pc' = [pc EXCEPT ![p] = "idle"]
  /\ UNCHANGED <<turn, vault_holder, ops_left>>

Next == \E p \in Procs :
  \/ RequestAccess(p) \/ PassTurn(p) \/ EnterVault(p)
  \/ ExecuteOp(p) \/ FinishWork(p) \/ ReleaseAccess(p)

====