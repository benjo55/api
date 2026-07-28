# Module Processus & Workflows

Ce module ajoute une gestion de processus métiers et de workflows dans la cartographie SI.

## Objectifs

- Décrire un processus métier versionné.
- Dessiner un workflow avec couloirs d’acteurs, tâches et transitions.
- Valider la cohérence avant publication.
- Publier une seule version courante par processus.
- Exécuter un runtime minimal : démarrage d’instance, avancement des tâches automatiques, complétion des tâches humaines.

## Modèle SQL Server

Les tables sont créées dans le schéma `workflow`.

- `ProcessDefinitions` : définition métier stable du processus.
- `ProcessVersions` : versions brouillon/publiées, avec coordonnées de canvas.
- `WorkflowLanes` : couloirs / acteurs responsables.
- `WorkflowTasks` : étapes du workflow (`Start`, `End`, `Human`, `Machine`, `Gateway`, `SubProcess`).
- `WorkflowTransitions` : liens orientés entre tâches.
- `ProcessInstances` : instances d’exécution.
- `WorkflowTaskInstances` : tâches d’exécution.
- `WorkflowEventLogs` : journal d’événements.

Règles importantes :

- unicité des codes hors suppression logique ;
- une seule version courante par processus ;
- impossibilité de publier une version invalide ;
- suppression d’un processus en soft delete ;
- le journal d’événements ne cascade pas, afin d’éviter les chemins de suppression multiples SQL Server.

## Validation

La validation vérifie notamment :

- exactement une tâche `Start` ;
- au moins une tâche `End` ;
- pas de code de tâche ou de couloir en doublon ;
- transitions source/cible valides ;
- pas de boucle directe source = cible ;
- départ sans entrant, fin sans sortant ;
- toutes les tâches accessibles depuis le départ ;
- toutes les tâches peuvent atteindre une fin.

## API

Principaux endpoints :

- `GET /api/processes`
- `POST /api/processes`
- `GET /api/processes/{id}`
- `PUT /api/processes/{id}`
- `DELETE /api/processes/{id}`
- `GET /api/processes/{processId}/versions`
- `POST /api/processes/{processId}/versions`
- `POST /api/process-versions/{versionId}/duplicate`
- `GET /api/process-versions/{versionId}/diagram`
- `PUT /api/process-versions/{versionId}/diagram`
- `POST /api/process-versions/{versionId}/validate`
- `POST /api/process-versions/{versionId}/publish`
- `POST /api/process-versions/{versionId}/instances`
- `GET /api/process-instances/{instanceId}`
- `POST /api/task-instances/{taskInstanceId}/complete`

## Frontend

Le menu `Cartographie du SI` contient désormais `Processus & Workflows`.

Écrans :

- `/processes` : liste et création de processus ;
- `/processes/:processId` : fiche processus et versions ;
- `/workflow-designer/:versionId` : designer graphique React Flow.

Le designer permet :

- l’ajout de couloirs ;
- l’ajout de tâches `Start`, `End`, humaine, machine, gateway ;
- la création de transitions par glisser-déposer entre tâches ;
- l’édition de propriétés principales ;
- la sauvegarde du diagramme ;
- la validation et la publication.

