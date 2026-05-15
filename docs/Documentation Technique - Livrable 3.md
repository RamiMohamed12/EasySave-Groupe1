# Documentation Technique - Livrable 3

## Nouveautés runtime

Le socle Livrable 3 ajoute un moteur d'exécution plus riche dans `EasySave.Core` :

- planification parallèle des sauvegardes
- arbitrage global des fichiers prioritaires
- limitation globale des gros fichiers selon un seuil configurable
- contrôle runtime par job : pause, reprise, arrêt
- pause automatique sur détection d'un logiciel métier bloquant

## Paramètres runtime

Les paramètres suivants sont persistés dans `storage-settings.json` :

- `PriorityExtensions`
- `LargeFileThresholdKb`
- `MaxConcurrentJobs`

Ils s'ajoutent aux paramètres déjà présents :

- `StorageDirectory`
- `LanguageCode`
- `LogFileFormat`
- `EncryptedExtensions`
- `CryptoSoftKey`
- `CryptoSoftPath`
- `BlockedProcessNames`
- `ThemeMode`

## Règles métier globales

- Tant qu'un fichier prioritaire reste en attente, aucun fichier non prioritaire ne doit démarrer.
- Deux fichiers dont la taille dépasse `LargeFileThresholdKb` ne peuvent pas être transférés en même temps.
- Les petits fichiers peuvent continuer à tourner en parallèle si la règle de priorité est déjà satisfaite.
- Une pause causée par un logiciel métier est automatiquement levée quand le processus bloquant disparaît, sauf si l'utilisateur a demandé un arrêt manuel.

## Persistance critique

`jobs.json`, `state.json` et `backup-history.json` utilisent désormais :

- une écriture atomique
- un verrou local par fichier
- une courte stratégie de retry sur collision

L'objectif est de réduire les risques de corruption ou de lecture partielle pendant l'exécution concurrente.

## Surface WPF

L'interface WPF expose désormais :

- l'édition des règles runtime
- l'affichage du statut runtime des jobs
- l'affichage d'un pourcentage de progression
- l'affichage du fichier courant
- l'affichage du mode de transfert courant
- les actions `Pause`, `Resume`, `Stop`

