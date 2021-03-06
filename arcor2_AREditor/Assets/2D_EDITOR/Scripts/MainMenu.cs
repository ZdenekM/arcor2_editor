using UnityEngine;
using UnityEngine.UI;
using System;
using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using Base;
using DanielLochner.Assets.SimpleSideMenu;
using System.Threading.Tasks;

[RequireComponent(typeof(SimpleSideMenu))]
public class MainMenu : MonoBehaviour, IMenu {
    public GameObject ActionObjectButtonPrefab, ServiceButtonPrefab;
    public GameObject ProjectControlButtons, ActionObjectsContent, ActionObjects,
        SceneControlButtons, MainControlButtons, Services, ServicesContent, RunningProjectControls;
    public GameObject PauseBtn, ResumeBtn;

    [SerializeField]
    private ButtonWithTooltip CloseProjectBtn, CloseSceneBtn, BuildAndRunBtn, BuildBtn, SaveProjectBtn, SaveSceneBtn;

    public ServiceSettingsDialog ServiceSettingsDialog;
    public AutoAddObjectDialog AutoAddObjectDialog;
    public AddSerivceDialog AddNewServiceDialog;

    private GameObject debugTools;

    private Dictionary<string, ServiceButton> serviceButtons = new Dictionary<string, ServiceButton>();

    [SerializeField]
    private InputDialog inputDialog;

    [SerializeField]
    private ConfirmationDialog confirmationDialog;

    private SimpleSideMenu menu;

    [SerializeField]
    private GameObject loadingScreen;

    private bool unsavedChanges = false;

    // Start is called before the first frame update
    private void Start() {
        menu = GetComponent<SimpleSideMenu>();
        Debug.Assert(ActionObjectButtonPrefab != null);
        Debug.Assert(ServiceButtonPrefab != null);
        Debug.Assert(ProjectControlButtons != null);
        Debug.Assert(ActionObjectsContent != null);
        Debug.Assert(ActionObjects != null);
        Debug.Assert(SceneControlButtons != null);
        Debug.Assert(MainControlButtons != null);
        Debug.Assert(Services != null);
        Debug.Assert(ServicesContent != null);
        Debug.Assert(RunningProjectControls != null);
        Debug.Assert(ServiceSettingsDialog != null);
        Debug.Assert(AutoAddObjectDialog != null);
        Debug.Assert(AddNewServiceDialog != null);
        Debug.Assert(inputDialog != null);
        Debug.Assert(confirmationDialog != null);
        Debug.Assert(ResumeBtn != null);
        Debug.Assert(PauseBtn != null);
        Debug.Assert(loadingScreen != null);


        Base.GameManager.Instance.OnConnectedToServer += ConnectedToServer;
        Base.SceneManager.Instance.OnServicesUpdated += ServicesUpdated;
        Base.ActionsManager.Instance.OnActionObjectsUpdated += ActionObjectsUpdated;
        Base.ActionsManager.Instance.OnServiceMetadataUpdated += ServiceMetadataUpdated;
        Base.GameManager.Instance.OnGameStateChanged += GameStateChanged;
        //Base.GameManager.Instance.OnProjectStateChanged += ProjectStateChanged;
        Base.GameManager.Instance.OnRunPackage += OnOpenProjectRunning;
        Base.GameManager.Instance.OnPausePackage += OnPausePackage;
        Base.GameManager.Instance.OnResumePackage += OnResumePackage;
        Base.GameManager.Instance.OnOpenSceneEditor += OnOpenSceneEditor;
        Base.GameManager.Instance.OnOpenProjectEditor += OnOpenProjectEditor;
        //Base.GameManager.Instance.OnOpenMainScreen += OnOpenMainScreen;
        Base.GameManager.Instance.OnDisconnectedFromServer += OnOpenDisconnectedScreen;
        Base.SceneManager.Instance.OnSceneChanged += OnSceneOrProjectChanged;
        Base.ProjectManager.Instance.OnProjectChanged += OnSceneOrProjectChanged;
        Base.SceneManager.Instance.OnSceneSaved += OnSceneOrProjectSaved;
        Base.ProjectManager.Instance.OnProjectSaved += OnSceneOrProjectSaved;


        HideEverything();
        OnOpenDisconnectedScreen(this, EventArgs.Empty);
        MenuManager.Instance.ShowMenu(MenuManager.Instance.MainMenu);

        debugTools = GameObject.FindGameObjectWithTag("debug_tools");
        if (debugTools != null)
            debugTools.SetActive(false);
    }

    private void OnSceneOrProjectSaved(object sender, EventArgs e) {
        unsavedChanges = false;
        _ = UpdateBuildAndSaveBtns();
    }

    private void OnSceneOrProjectChanged(object sender, EventArgs e) {
        unsavedChanges = true;
        _ = UpdateBuildAndSaveBtns();
    }

    private void OnResumePackage(object sender, ProjectMetaEventArgs args) {
        ResumeBtn.SetActive(false);
        PauseBtn.SetActive(true);
    }

    private void OnPausePackage(object sender, ProjectMetaEventArgs args) {
        PauseBtn.SetActive(false);
        ResumeBtn.SetActive(true);
    }

    private void OnOpenProjectRunning(object sender, ProjectMetaEventArgs args) {
        RunningProjectControls.SetActive(true);
        ResumeBtn.SetActive(false);
        PauseBtn.SetActive(true);
    }

    private void GameStateChanged(object sender, Base.GameStateEventArgs args) {
        HideEverything();
    }

    private void OnOpenMainScreen(object sender, EventArgs eventArgs) {
        MainControlButtons.SetActive(true);
    }

    private void OnOpenSceneEditor(object sender, EventArgs eventArgs) {
        unsavedChanges = true;
        SceneControlButtons.SetActive(true);
        ActionObjects.SetActive(true);
        ServicesUpdated(null, new Base.ServiceEventArgs(null));
        Services.SetActive(true);
    }

    private void OnOpenProjectEditor(object sender, EventArgs eventArgs) {
        unsavedChanges = true;
        ProjectControlButtons.SetActive(true);
        ServicesUpdated(null, new Base.ServiceEventArgs(null));
        Services.SetActive(true);
        if (ProjectManager.Instance.Project.HasLogic) {
            BuildAndRunBtn.SetInteractivity(true);
        } else {
            BuildAndRunBtn.SetInteractivity(false, "Project without defined logic could not be run from editor");
        }
    }



    private void OnOpenDisconnectedScreen(object sender, EventArgs eventArgs) {

    }


    private void HideEverything() {
        ProjectControlButtons.SetActive(false);
        ActionObjects.SetActive(false);
        SceneControlButtons.SetActive(false);
        MainControlButtons.SetActive(false);
        Services.SetActive(false);
        RunningProjectControls.SetActive(false);
    }

    private void ActionObjectsUpdated(object sender, Base.StringEventArgs eventArgs) {

        foreach (ActionObjectButton b in ActionObjectsContent.GetComponentsInChildren<ActionObjectButton>()) {
            if (b.gameObject.tag == "PersistentButton") {
                continue;
            } else {
                Destroy(b.gameObject);
            }

        }
        foreach (Base.ActionObjectMetadata actionObjectMetadata in Base.ActionsManager.Instance.ActionObjectMetadata.Values) {
            if (Base.ActionsManager.Instance.ActionObjectMetadata.TryGetValue(actionObjectMetadata.Type, out Base.ActionObjectMetadata actionObject)) {
                if (actionObject.Abstract) {
                    continue;
                }
            } else {
                continue;
            }

            GameObject btnGO = Instantiate(ActionObjectButtonPrefab, ActionObjectsContent.transform);
            ActionObjectButton btn = btnGO.GetComponent<ActionObjectButton>();
            btn.SetLabel(actionObjectMetadata.Type);
            btn.Button.onClick.AddListener(() => AddObjectToScene(actionObjectMetadata.Type));
            btn.RemoveBtn.Button.onClick.AddListener(() => ShowRemoveActionObjectDialog(actionObjectMetadata.Type));
            btn.RemoveBtn.SetInteractivity(false, "");
            btnGO.transform.SetAsFirstSibling();

            if (eventArgs.Data == actionObjectMetadata.Type) {
                btn.GetComponent<ActionButton>().Highlight(2f);
            }
            btn.Button.interactable = !actionObjectMetadata.Disabled;

        }

        UpdateRemoveBtns();

    }

    public void ServicesUpdated(object sender, Base.ServiceEventArgs eventArgs) {
        if (eventArgs.Data != null) {
            if (serviceButtons.TryGetValue(eventArgs.Data.Data.Type, out ServiceButton btn)) {
                UpdateServiceButton(btn);
            }
        } else {
            foreach (ServiceButton serviceButton in serviceButtons.Values) {
                UpdateServiceButton(serviceButton);
            }
        }
        _ = UpdateBuildAndSaveBtns();

    }

    public void ShowRemoveActionObjectDialog(string type) {
        confirmationDialog.Open("Delete object",
                         "Are you sure you want to delete action object " + type + "?",
                         () => RemoveActionObject(type),
                         () => confirmationDialog.Close());
    }

    public async void RemoveActionObject(string type) {
        try {
            await WebsocketManager.Instance.DeleteObjectType(type, false);
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to remove object type.", ex.Message);
            Debug.LogError(ex);
        } finally {
            confirmationDialog.Close();
        }
    }

    private static void UpdateServiceButton(ServiceButton serviceButton) {
        serviceButton.SetInteractable(!serviceButton.ServiceMetadata.Disabled);

        if (Base.SceneManager.Instance.ServiceInScene(serviceButton.ServiceMetadata.Type)) {
            //checked
            serviceButton.gameObject.SetActive(true);
            serviceButton.State = true;
        } else {
            if (Base.GameManager.Instance.GetGameState() == Base.GameManager.GameStateEnum.ProjectEditor) {
                serviceButton.gameObject.SetActive(false);
            } else {
                serviceButton.gameObject.SetActive(true);
            }
            serviceButton.State = false;
        }
    }

    public void ServiceMetadataUpdated(object sender, EventArgs e) {

        foreach (ServiceButton b in serviceButtons.Values) {
            Destroy(b.gameObject);
        }

        serviceButtons.Clear();

        foreach (IO.Swagger.Model.ServiceTypeMeta service in Base.ActionsManager.Instance.ServicesMetadata.Values) {
            ServiceButton serviceButton = Instantiate(ServiceButtonPrefab).GetComponent<ServiceButton>();
            serviceButton.transform.SetParent(ServicesContent.transform);
            serviceButton.transform.localScale = new Vector3(1, 1, 1);
            serviceButton.gameObject.GetComponentInChildren<TMPro.TMP_Text>().text = service.Type;

            serviceButton.ServiceMetadata = service;
            serviceButton.gameObject.GetComponentInChildren<Button>().onClick.AddListener(() => ServiceStateChanged(serviceButton.GetComponent<ServiceButton>()));
            serviceButton.transform.SetAsLastSibling();
            serviceButtons.Add(service.Type, serviceButton);
        }
        ServicesUpdated(null, new Base.ServiceEventArgs(null));
    }

    public void ServiceStateChanged(ServiceButton serviceButton) {
        if (Base.GameManager.Instance.GetGameState() == Base.GameManager.GameStateEnum.ProjectEditor) {
            Base.Notifications.Instance.ShowNotification("Failed to update service", "Service state can only be changed in scene editor!");
            return;
        }
        if (!serviceButton.State) {
            ShowAddServiceDialog(serviceButton.ServiceMetadata.Type);
        } else {
            ShowServiceSettingsDialog(serviceButton);
        }
    }

    private void AddObjectToScene(string type) {
        if (Base.ActionsManager.Instance.ActionObjectMetadata.TryGetValue(type, out Base.ActionObjectMetadata actionObjectMetadata)) {
            if (actionObjectMetadata.NeedsServices.Count > 0) {
                ShowAutoAddObjectDialog(type);
            } else {
                ShowAddObjectDialog(type);
            }
        } else {
            Base.NotificationsModernUI.Instance.ShowNotification("Failed to add object", "Object type " + type + " does not exist!");
        }

    }

    public async void ShowCloseSceneDialog() {
        (bool success, _) = await Base.GameManager.Instance.CloseScene(false);
        if (!success) {
            GameManager.Instance.HideLoadingScreen();
            confirmationDialog.Open("Close scene",
                         "Are you sure you want to close current scene? Unsaved changes will be lost.",
                         () => CloseScene(),
                         () => confirmationDialog.Close());
        }
    }


    public async void CloseScene() {
        (bool success, string message) = await Base.GameManager.Instance.CloseScene(true);
        if (success) {
            confirmationDialog.Close();
            MenuManager.Instance.MainMenu.Close();
        }
    }


    public async void ShowCloseProjectDialog() {
        (bool success, _) = await Base.GameManager.Instance.CloseProject(false);
        if (!success) {
            GameManager.Instance.HideLoadingScreen();
            confirmationDialog.Open("Close project",
                         "Are you sure you want to close current project? Unsaved changes will be lost.",
                         () => CloseProject(),
                         () => confirmationDialog.Close());
        }

    }

    public async void CloseProject() {
        GameManager.Instance.ShowLoadingScreen("Closing project..");
        _ = await GameManager.Instance.CloseProject(true);
        inputDialog.Close();
        MenuManager.Instance.MainMenu.Close();
        GameManager.Instance.HideLoadingScreen();
    }


    public void ShowAddObjectDialog(string type) {
        inputDialog.Open("Add object of type " + type,
                         "",
                         "Object name",
                         SceneManager.Instance.GetFreeAOName(type),
                         () => AddObject(type, inputDialog.GetValue()),
                         () => inputDialog.Close());
    }

    public async void AddObject(string type, string name) {
        if (await Base.GameManager.Instance.AddObjectToScene(type: type, name: name)) {
            inputDialog.Close();
        }

    }


    public void ShowAutoAddObjectDialog(string type) {
        AutoAddObjectDialog.ObjectToBeAdded = type;
        AutoAddObjectDialog.Open();
    }

    public void ShowAddServiceDialog(string type) {
        AddNewServiceDialog.UpdateMenu(type);
        AddNewServiceDialog.Open();
    }

    public void ShowServiceSettingsDialog(ServiceButton serviceButton) {
        bool sceneEditor = Base.GameManager.Instance.GetGameState() == Base.GameManager.GameStateEnum.SceneEditor;
        ServiceSettingsDialog.Show(serviceButton.ServiceMetadata.Type, sceneEditor);
    }

    public void ShowProjectControlButtons() {
        ProjectControlButtons.SetActive(true);
    }

    public void ShowDynamicContent() {
        ActionObjects.SetActive(true);
    }

    public void HideProjectControlButtons() {
        ProjectControlButtons.SetActive(false);
    }

    public void HideDynamicContent() {
        ActionObjects.SetActive(false);
    }


    public void ConnectedToServer(object sender, Base.StringEventArgs e) {
        ShowProjectControlButtons();
        ShowDynamicContent();
    }



    public void ProjectRunning(object sender, EventArgs e) {

    }

    public void ShowNewObjectTypeMenu() {
        MenuManager.Instance.ShowMenu(MenuManager.Instance.NewObjectTypeMenu);
    }

    public async void SaveScene() {
        IO.Swagger.Model.SaveSceneResponse saveSceneResponse = await Base.GameManager.Instance.SaveScene();
        if (!saveSceneResponse.Result) {
            saveSceneResponse.Messages.ForEach(Debug.LogError);
            Notifications.Instance.ShowNotification("Scene save failed", saveSceneResponse.Messages.Count > 0 ? saveSceneResponse.Messages[0] : "Failed to save scene");
            return;
        } else {
            SaveSceneBtn.SetInteractivity(false, "There are no unsaved changes");
            _ = UpdateBuildAndSaveBtns();
        }
        Notifications.Instance.ShowNotification("Scene save sucessfull", "");
    }

    public async void SaveProject() {
        IO.Swagger.Model.SaveProjectResponse saveProjectResponse = await Base.GameManager.Instance.SaveProject();
        if (!saveProjectResponse.Result) {
            saveProjectResponse.Messages.ForEach(Debug.LogError);
            Base.Notifications.Instance.ShowNotification("Failed to save project", (saveProjectResponse.Messages.Count > 0 ? ": " + saveProjectResponse.Messages[0] : ""));
            return;
        }
        UpdateMenu();
        Base.Notifications.Instance.ShowNotification("Project saved successfully", "");
    }



    public void ShowBuildPackageDialog() {
        inputDialog.Open("Build package",
                         "",
                         "Package name",
                         Base.ProjectManager.Instance.Project.Name + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"),
                         () => BuildPackage(inputDialog.GetValue()),
                         () => inputDialog.Close());
    }

    public async void BuildPackage(string name) {
        try {
            await Base.GameManager.Instance.BuildPackage(name);
            inputDialog.Close();
        } catch (Base.RequestFailedException ex) {

        }

    }

    public void ShowBuildAndRunPackage() {
        inputDialog.Open("Build and run package",
                         "",
                         "Package name",
                         Base.ProjectManager.Instance.Project.Name + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"),
                         () => BuildAndRunPackage(inputDialog.GetValue()),
                         () => inputDialog.Close());
    }

    public async void BuildAndRunPackage(string name) {
        inputDialog.Close();
        if (await Base.GameManager.Instance.BuildAndRunPackage(name)) {



        } else {
            Base.Notifications.Instance.ShowNotification("Failed to build and run package", "");
            GameManager.Instance.HideLoadingScreen();
        }
    }

    public void TestRun() {
        Base.GameManager.Instance.TestRunProject();
    }

    public void StopProject() {
        Base.GameManager.Instance.StopProject();
    }


    public void PauseProject() {
        Base.GameManager.Instance.PauseProject();
    }

    public void ResumeProject() {
        Base.GameManager.Instance.ResumeProject();
    }

    public void DisconnectFromSever() {
        Base.GameManager.Instance.DisconnectFromSever();
    }

    public void ExitApp() {
        Base.GameManager.Instance.ExitApp();
    }

    public void SetDebugMode() {
        if (debugTools != null) {
            if (debugTools.activeSelf)
                debugTools.SetActive(false);
            else
                debugTools.SetActive(true);
        }
    }

    public async void UpdateMenu() {
        if (menu.CurrentState == SimpleSideMenu.State.Open) {
            menu.Close();
            return;
        } else {
            loadingScreen.SetActive(true);
            menu.Open();
        }

        await UpdateBuildAndSaveBtns();
        UpdateRemoveBtns();
        loadingScreen.SetActive(false);
    }

    public async Task UpdateBuildAndSaveBtns() {
        bool success = false, successForce = false;
        string messageForce = "";
        ButtonWithTooltip button = null;
        switch (GameManager.Instance.GetGameState()) {
            case GameManager.GameStateEnum.ProjectEditor:
                (successForce, messageForce) = await GameManager.Instance.CloseProject(true, true);
                button = CloseProjectBtn;

                if (!unsavedChanges) {
                    BuildBtn.SetInteractivity(true);
                    SaveProjectBtn.SetInteractivity(false, "There are no unsaved changes");
                    if (ProjectManager.Instance.Project.HasLogic)
                        BuildAndRunBtn.SetInteractivity(true);
                } else {
                    BuildBtn.SetInteractivity(false, "There are unsaved changes on project");
                    BuildAndRunBtn.SetInteractivity(false, "There are unsaved changes on project");
                    SaveProjectBtn.SetInteractivity(true);
                }
                break;
            case GameManager.GameStateEnum.SceneEditor:
                (successForce, messageForce) = await GameManager.Instance.CloseScene(true, true);
                button = CloseSceneBtn;
                if (!unsavedChanges) {
                    SaveSceneBtn.SetInteractivity(false, "There are no unsaved changes");
                } else {
                    SaveSceneBtn.SetInteractivity(true);
                }
                break;
        }
        if (button != null) {
            if (successForce) {
                button.SetInteractivity(true);
            } else {
                button.SetInteractivity(false, messageForce);
            }
        }
    }

    public async void UpdateRemoveBtns() {
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.SceneEditor) {
            return;
        }
        foreach (ActionObjectButton b in ActionObjectsContent.GetComponentsInChildren<ActionObjectButton>()) {
            if (b == null || b.RemoveBtn == null)
                return;
            try {
                await WebsocketManager.Instance.DeleteObjectType(b.GetLabel(), true);
                b.RemoveBtn.SetInteractivity(true);
            } catch (RequestFailedException ex) {
                b.RemoveBtn.SetInteractivity(false, ex.Message);
            }
        }
    }

    public void SetHeader(string header) {
        //nothing to do.. yet
    }

    public void SaveLogs() {
        Base.Notifications.Instance.SaveLogs(Base.SceneManager.Instance.Scene, Base.ProjectManager.Instance.Project);
    }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    public void Recalibrate() {
        CalibrationManager.Instance.Recalibrate();
    }
#endif

 
}
