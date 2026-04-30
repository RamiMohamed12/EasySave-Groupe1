**Documentation Technique**

Le projet est maintenant une solution .NET 8 contenant deux modes : console et graphique, avec une séparation assez nette entre métier, interfaces et persistance : 
- `EasySave.Core` porte la logique de sauvegarde, 
- `EasyLog` gère les logs et les chemins de stockage, 
- `EasySave.Console` reste l’interface terminal/TUI historique, 
- `EasySave.Wpf` ajoute la nouvelle interface graphique Windows, 
- `EasySave.Tests` couvre surtout le cœur applicatif.

L’entrée console instancie les services partagés une seule fois puis choisit entre le menu interactif et l’exécution par arguments selon `args.Length`. 
L’entrée WPF ouvre directement `MainWindow`, qui reconstruit la même pile métier (`BackupJobRegistry`, `StateService`, `BackupService`, `BackupController`, `ApplicationTextService`) dans le code-behind, sans couche MVVM dédiée pour l’UI graphique.

**Architecture Métier**

`BackupJobRegistry` maintient exactement 5 slots de sauvegarde, normalisés à chaque chargement. Les noms et types sont reconstruits par défaut (`Job1` à `Job5`, alternance `Full`/`Differential`), ce qui signifie qu’en pratique seules les colonnes `Source` et `Target` sont réellement configurables depuis les interfaces.

`BackupService` exécute les copies fichier par fichier. Il valide les chemins, calcule les fichiers éligibles, met à jour l’état courant, journalise chaque transfert et marque la sauvegarde en succès ou erreur à la fin. Le mode différentiel repose sur `backup-history.json` : après une sauvegarde complète réussie, un timestamp UTC est mémorisé, puis utilisé pour ne recopier que les fichiers modifiés ou absents côté cible.

`StateService` maintient un snapshot global `state.json` pour tous les jobs, pas seulement pour celui en cours. Lorsqu’un job évolue, l’état du job est remplacé puis resynchronisé avec la configuration connue afin de conserver les 5 entrées logiques.

`RuntimeStoragePaths` centralise la persistance d’exécution. `storage-settings.json` reste au répertoire de base de l’application, tandis que `jobs.json`, `state.json`, `backup-history.json` et les logs journaliers `.json` ou `.xml` sont redirigés vers le dossier de stockage configuré. La langue et le format de log y sont aussi persistés.

**2.0 - Ajout De L’Interface WPF**

La fenêtre WPF est organisée en 3 zones fonctionnelles : tableau des jobs configurés, panneau d’édition du job sélectionné, et panneaux de visualisation directe de `state.json` et du log du jour.

Techniquement, l’UI WPF n’introduit pas une nouvelle logique métier : elle orchestre les mêmes services que la console. Le flux principal est le suivant :
1. chargement des jobs dans une liste `JobRow` pour le `DataGrid` ;
2. enregistrement des modifications vers `jobs.json` via `BackupJobRegistry` ;
3. lancement des jobs cochés ou de tous les jobs via `BackupController` ;
4. rafraîchissement de `state.json` et du log du jour dans deux `TextBox` en lecture seule.

Le lancement des sauvegardes se fait en `Task.Run(...)` pour éviter de bloquer l’interface, avec un verrou applicatif simple `_isBusy` qui désactive les actions principales pendant l’exécution. En revanche, il n’y a pas de progression temps réel bindée depuis `BackupState` vers l’UI : la fenêtre affiche surtout un état “après action” via relecture de fichiers.

La localisation WPF réutilise la même ressource partagée que la console via `ApplicationTextService`. La priorité est : variable d’environnement `EASYSAVE_LANGUAGE`, puis langue persistée, puis culture système ; WPF peut ensuite changer la langue à chaud via le `ComboBox`, qui persiste le choix et recrée le contrôleur métier.

**Tests Et Points D’Attention**

La couverture automatisée est bonne sur le noyau : registre des jobs, parsing CLI, chemins de stockage, logs, historique différentiel, état et copie effective sont testés. En revanche, je n’ai pas trouvé de tests d’interface WPF proprement dits ; le seul lien automatisé avec WPF que j’ai vu est la vérification d’une clé de ressource texte.

Point d’attention important après l’ajout de WPF : le projet graphique cible `net8.0-windows` avec `UseWPF=true`, alors que la CI GitHub Actions reste configurée sur `ubuntu-latest` pour restaurer, builder et tester toute la solution. C’est cohérent fonctionnellement pour le poste Windows utilisateur, mais c’est un sujet de compatibilité CI à surveiller.
