# EasySave - Groupe 1

EasySave est une solution .NET 8 de sauvegarde proposant trois modes d’utilisation : une application graphique WPF, une interface console/TUI interactive et une utilisation en ligne de commande.
Elle prend en charge les sauvegardes complètes et différentielles, conserve l'état d'exécution au format JSON et génère des journaux de transfert quotidiens avec la bibliothèque `EasyLog`.

La version `1.1.0` introduit une nouvelle interface TUI plus graphique, basée sur des menus interactifs au clavier.

La branche de travail Livrable 3 introduit aussi des capacités runtime avancées :

* planification parallèle contrôlée des sauvegardes
* extensions prioritaires configurables
* seuil configurable pour les gros fichiers
* commandes `Pause`, `Resume` et `Stop` par travail
* pause automatique si un logiciel métier bloquant est détecté
* affichage temps réel de l'état et de la progression dans l'interface WPF

---

## Sommaire

* [Fonctionnalités](#fonctionnalités)
* [Mode TUI graphique](#mode-tui-graphique)
* [Utilisation en ligne de commande](#utilisation-en-ligne-de-commande)
* [Mode graphique WPF](#mode-graphique-wpf)
* [Fichiers d'exécution](#fichiers-dexécution)
* [Support des langues](#support-des-langues)
* [Documentation](#documentation)
* [Contributeurs](#contributeurs)
* [Remarques](#remarques)

---

## Fonctionnalités

Avec EasySave, vous pouvez :

* Gérer jusqu'à 5 tâches de sauvegarde.
* Configurer les chemins source et cible de chaque tâche.
* Lancer une tâche unique, une plage de tâches ou une sélection personnalisée.
* Choisir entre une sauvegarde complète (`Full`) ou différentielle (`Differential`).
* Utiliser une interface TUI avec navigation au clavier.
* Voir les tâches, l'état des sauvegardes et les logs depuis l'application.
* Choisir si les logs quotidiens sont générés en `JSON` ou en `XML`.
* Utiliser l'application en français ou en anglais.
* Définir un répertoire de stockage personnalisé pour les fichiers d'exécution.

---

## Mode TUI graphique

Quand `EasySave.exe` est lancé sans argument, l'application ouvre le nouveau menu interactif.

<img width="1092" height="697" alt="image" src="https://github.com/user-attachments/assets/fde90f36-74c1-4eb4-a81c-fc7dc4971f70" />


Le mode TUI permet de piloter l'application sans retenir les commandes :

* `View jobs` affiche les cinq tâches configurées.
* `Configure job` permet de modifier les chemins source et cible d'une tâche.
* `Run backups` lance une ou plusieurs sauvegardes.
* `View state` affiche l'état d'exécution des sauvegardes.
* `View logs` consulte les journaux générés.
* `Change log format` bascule entre les logs `JSON` et `XML`.
* `Change language` bascule l'interface entre français et anglais.

Navigation :

* `Up` / `Down` : déplacer la sélection.
* `Enter` : valider.
* `Esc` : revenir en arrière.


---

## Utilisation en ligne de commande

Les commandes suivantes permettent d'utiliser EasySave directement depuis un terminal.

### Lancer le mode TUI

```powershell
.\EasySave.exe
```

### Afficher l'aide

```powershell
.\EasySave.exe --help
```

### Exécuter une tâche

```powershell
.\EasySave.exe 2
```

### Exécuter une plage de tâches

```powershell
.\EasySave.exe 1-3
```

### Exécuter une liste personnalisée

```powershell
.\EasySave.exe "1;3;5"
```

### Configurer un chemin source

```powershell
.\EasySave.exe --configure 1 source "C:\SourceFolder"
```

### Configurer un chemin cible

```powershell
.\EasySave.exe --configure 1 target "D:\BackupFolder"
```

### Changer la langue

```powershell
.\EasySave.exe --lang fr
```

Utilisez `fr` pour le français ou `en` pour l'anglais.
Le choix est enregistré et réutilisé au prochain lancement.

### Modifier le répertoire de stockage

```powershell
.\EasySave.exe --storage-dir "C:\EasySaveData"
```

Cette commande déplace automatiquement les fichiers d'exécution :

* `jobs.json`
* `state.json`
* `backup-history.json`
* les logs quotidiens, par exemple `2026-04-29.json` ou `2026-04-29.xml`

---
## Mode graphique WPF

EasySave propose désormais une application graphique Windows basée sur WPF, en complément du mode console/TUI et de l’utilisation en ligne de commande.

L’application WPF permet de piloter les sauvegardes depuis une fenêtre dédiée, sans saisir de commandes. Elle réutilise la même logique métier que la console grâce au projet `EasySave.Core`, ce qui garantit un comportement cohérent entre les deux modes.

### Lancer l’application WPF

Depuis la racine du projet :

```powershell
dotnet run --project .\EasySave.Wpf
```

Ou depuis une version compilée : 
```powershell
.\EasySave.Wpf.exe
```

### Fonctionnalités du mode graphique


Le mode WPF permet de :

- Visualiser les tâches de sauvegarde dans un tableau ;
- Sélectionner une ou plusieurs tâches à exécuter ;
- Lancer uniquement les tâches cochées ou toutes les tâches ;
- Modifier les chemins source et cible d’une tâche ;
- Choisir le type de sauvegarde : complète ou différentielle ;
- Ajouter ou supprimer une tâche depuis l’interface ;
- Configurer les extensions à chiffrer avec CryptoSoft ;
- Renseigner la clé CryptoSoft utilisée pour le chiffrement ;
- Définir des extensions prioritaires ;
- Définir un seuil de gros fichiers en Ko ;
- Définir le nombre maximal de travaux concurrents ;
- Mettre en pause, reprendre ou arrêter les travaux sélectionnés ;
- Consulter directement le contenu de state.json ;
- Consulter le journal du jour au format JSON ou XML ;
- Changer la langue de l’interface entre français et anglais ;
- Rafraîchir l’état de l’application après une exécution.


*Pendant l’exécution d’une sauvegarde, l’interface désactive les actions principales afin d’éviter les conflits. Les traitements sont lancés en arrière-plan pour conserver une fenêtre réactive.*


---

## Fichiers d'exécution

Par défaut, les fichiers sont stockés dans le dossier de l'application.

### `jobs.json`

Contient la configuration des cinq tâches de sauvegarde : nom, chemin source, chemin cible et type de sauvegarde.

### `state.json`

Contient le dernier état connu des sauvegardes :

* nom de la tâche
* chemins source et destination
* statut
* nombre de fichiers et d'octets transférés/restants
* horodatages associés

### `backup-history.json`

Utilisé par les sauvegardes différentielles pour retrouver la dernière sauvegarde complète réussie.

### `yyyy-MM-dd.json` / `yyyy-MM-dd.xml`

Contient les journaux quotidiens. Selon le format choisi, EasySave écrit les logs en JSON ou en XML.

### `yyyy-MM-dd.jsonl` sur le serveur Docker

Le service `EasyLog.Server` centralise les logs de plusieurs postes dans un seul fichier journalier JSONL. Chaque ligne contient une entree `LogEntry` avec `UserName`, `MachineName` et `ClientId`, ce qui permet de differencier les utilisateurs dans le meme fichier.

Modes disponibles dans EasySave Console et WPF :

* `Local` : les logs restent uniquement sur le PC utilisateur.
* `Centralized` : les logs sont envoyes uniquement au serveur Docker.
* `Local + Centralized` : les logs sont ecrits localement et envoyes au serveur Docker.

Lancer le serveur depuis la racine du projet :

```powershell
docker compose up --build -d easylog-server
```

Verifier le service :

```powershell
curl http://localhost:5080/health
```

Configurer chaque poste EasySave :

* mode logs : `Centralized` ou `Local + Centralized`
* URL serveur : `http://<IP_DU_SERVEUR_DOCKER>:5080`
* utilisateur : nom lisible, par exemple `alice` ou `poste-compta-1`
* cle API : facultative, uniquement si `EASYLOG_API_KEY` est definie cote Docker

Consulter les logs Docker :

```powershell
docker compose logs -f easylog-server
```

Lire le fichier centralise :

```powershell
Get-Content .\docker-data\easylog\logs\<yyyy-MM-dd>.jsonl
```

Arreter le service :

```powershell
docker compose down
```

Si les postes EasySave sont sur d'autres machines, ouvrir le port `5080` sur le pare-feu du serveur Docker et utiliser l'adresse IP du serveur a la place de `localhost`.

---

## Support des langues

Le changement de langue peut se faire depuis le menu TUI ou directement en ligne de commande :

```powershell
.\EasySave.exe --lang en
```

ou

```powershell
.\EasySave.exe --lang fr
```

Ordre de priorité utilisé par l'application :

1. variable d'environnement `EASYSAVE_LANGUAGE`
2. langue enregistrée via `--lang` ou le menu TUI
3. langue du système

Pour forcer une langue pendant le développement :

```powershell
$env:EASYSAVE_LANGUAGE = "fr"
dotnet run --project .\EasySave.Console
```

---

## Documentation

Des ressources complémentaires sont disponibles dans le dossier [`docs`](./docs) :

* `PROJECT.md`
* `PROJECTSTRUCT.md`
* `LOGS.md`
* `Diagrammes UML.pdf`
* `Manuel Utilisateur.pdf`
* `Documentation Technique.pdf`

---

## Contributeurs

| RAMI Mohamed Amine | HALLAOUA Sidali | RADI Selma Meriem | AKROUR Abdenour |
| ------------------ | --------------- | ----------------- | --------------- |

---

## Remarques

* La version publiée contient une application en état neutre, sans logs personnels ni anciens états de sauvegarde.
* Vous pouvez utiliser des chemins locaux, des disques externes ou des emplacements réseau si vous disposez des droits d'accès nécessaires.
* Les tâches sont actuellement exécutées de manière séquentielle.

