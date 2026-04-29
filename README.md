# EasySave - Groupe 1

EasySave est une application console .NET 8 conçue pour vous permettre de gérer efficacement jusqu’à cinq tâches de sauvegarde.
Elle prend en charge les sauvegardes complètes et différentielles, conserve l’état d’exécution au format JSON et génère des journaux de transfert quotidiens via une bibliothèque de journalisation réutilisable nommée `EasyLog`.

---

## Sommaire

* [Fonctionnalités](#fonctionnalités)
* [Utilisation en ligne de commande](#utilisation-en-ligne-de-commande)

  * [Afficher les tâches configurées](#afficher-les-tâches-configurées)
  * [Afficher l’aide](#afficher-laide)
  * [Exécuter une tâche](#exécuter-une-tâche)
  * [Exécuter une plage de tâches](#exécuter-une-plage-de-tâches)
  * [Exécuter une liste personnalisée](#exécuter-une-liste-personnalisée)
  * [Configurer un chemin source](#configurer-un-chemin-source)
  * [Configurer un chemin cible](#configurer-un-chemin-cible)
  * [Changer la langue de l’application](#changer-la-langue-de-lapplication)
  * [Modifier le répertoire de stockage](#modifier-le-répertoire-de-stockage-à-lexécution)
* [Fichiers d’exécution](#fichiers-dexécution)
* [Support des langues](#support-des-langues)
* [Documentation](#documentation)
* [Contributeurs](#contributeurs)
* [Remarques](#remarques)

---

## Fonctionnalités

Avec EasySave, vous pouvez :

* Gérer jusqu’à 5 tâches de sauvegarde de manière centralisée
* Lancer facilement une tâche unique, plusieurs tâches consécutives ou une sélection personnalisée
* Choisir entre une sauvegarde complète (`Full`) ou différentielle (`Differential`) selon vos besoins
* Suivre en temps réel l’état de vos sauvegardes grâce au fichier `state.json`
* Consulter un historique détaillé des opérations dans des fichiers journaux quotidiens
* Bénéficier d’une interface disponible en français et en anglais
* Définir un répertoire de stockage personnalisé pour vos fichiers d’exécution
* Choisir si les fichiers de logs quotidiens sont générés en `JSON` ou en `XML`

---

## Utilisation en ligne de commande

Les commandes suivantes vous permettent d’interagir simplement avec l’application selon votre besoin.

### Afficher les tâches configurées

```powershell
.\EasySave.exe
```

---

### Afficher l’aide

```powershell
.\EasySave.exe --help
```

---

### Exécuter une tâche

```powershell
.\EasySave.exe 2
```

---

### Exécuter une plage de tâches

```powershell
.\EasySave.exe 1-3
```

---

### Exécuter une liste personnalisée

```powershell
.\EasySave.exe "1;3;5"
```

---

### Configurer un chemin source

```powershell
.\EasySave.exe --configure 1 source "C:\SourceFolder"
```

---

### Configurer un chemin cible

```powershell
.\EasySave.exe --configure 1 target "D:\BackupFolder"
```

---

### Changer la langue de l’application

```powershell
.\EasySave.exe --lang fr
```

Utilisez `fr` pour le français ou `en` pour l’anglais.

Le choix est enregistré et réutilisé au prochain lancement.

---

### Modifier le répertoire de stockage à l’exécution

```powershell
.\EasySave.exe --storage-dir "C:\EasySaveData"
```

Cette commande permet de déplacer automatiquement les fichiers suivants :

* `jobs.json`
* `state.json`
* `backup-history.json`
* les fichiers journaux quotidiens (ex : `2026-04-25.json`)

---

## Fichiers d’exécution

Par défaut, les fichiers sont stockés dans le répertoire de base de l’application. Vous pouvez toutefois modifier cet emplacement comme indiqué précédemment.

### `jobs.json`

Si vous souhaitez consulter ou modifier la configuration des tâches, ce fichier contient les cinq emplacements de sauvegarde ainsi que leurs paramètres.

---

### `state.json`

Si vous souhaitez suivre l’avancement d’une sauvegarde en cours ou consulter le dernier état connu, ce fichier inclut :

* le nom de la tâche
* les chemins source et destination
* le statut de la sauvegarde
* le nombre de fichiers et d’octets transférés/restants
* les horodatages associés

---

### `backup-history.json`

Si vous utilisez des sauvegardes différentielles, ce fichier vous permet d’identifier la dernière sauvegarde complète réussie.

---

### `yyyy-MM-dd.json`

Si vous souhaitez analyser en détail les opérations effectuées, les journaux quotidiens enregistrent chaque action sous forme de lignes JSON, par exemple :

* `FileTransfer`
* `CreateDirectory`
* `Error`

Si vous changez le format des logs depuis le menu interactif, ces fichiers peuvent aussi être générés au format `yyyy-MM-dd.xml`.

---

## Support des langues

Le changement de langue peut maintenant se faire directement en ligne de commande :

```powershell
.\EasySave.exe --lang en
```

ou

```powershell
.\EasySave.exe --lang fr
```

Le dernier choix est stocké dans `storage-settings.json` et conservé entre les exécutions.

Ordre de priorité utilisé par l’application :

1. variable d’environnement `EASYSAVE_LANGUAGE`
2. langue enregistrée via `--lang`
3. langue du système (fallback)

Si vous souhaitez forcer une langue spécifique :

```powershell
$env:EASYSAVE_LANGUAGE = "fr"
dotnet run --project .\EasySave
```

Utilisez `"en"` pour l’anglais ou `"fr"` pour le français.

---

## Documentation

Si vous souhaitez approfondir votre compréhension du projet, des ressources complémentaires sont disponibles dans le dossier [`docs`](./docs) :

* `PROJECT.md`
* `PROJECTSTRUCT.md`
* `LOGS.md`
* `Diagrammes UML.pdf`
* `Manuel Utilisateur.pdf`

---

## Contributeurs

| RAMI Mohamed Amine | HALLAOUA Sidali | RADI Selma Meriem | AKROUR Abdenour |
| ------------------ | --------------- | ----------------- | --------------- |

---

## Remarques

* Si vous souhaitez tester rapidement l’application, un exemple de sortie est déjà disponible dans `publish/EasySave/`.
* Vous pouvez utiliser des chemins locaux, des disques externes ou des emplacements réseau, à condition de disposer des droits d’accès nécessaires.
* Les tâches sont actuellement exécutées de manière séquentielle.

---