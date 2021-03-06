using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Playables;
using Base;

public class MainScreen : Base.Singleton<MainScreen>
{
    public TMPro.TMP_Text ScenesBtn, ProjectsBtn, PackagesBtn;
    public GameObject SceneTilePrefab, TileNewPrefab, ProjectTilePrefab, PackageTilePrefab, ScenesDynamicContent, ProjectsDynamicContent, PackagesDynamicContent;
    public NewProjectDialog NewProjectDialog;
    public InputDialog InputDialog;

    [SerializeField]
    private SceneOptionMenu SceneOptionMenu;

    [SerializeField]
    private ProjectOptionMenu ProjectOptionMenu;

    [SerializeField]
    private PackageOptionMenu PackageOptionMenu;

    [SerializeField]
    private CanvasGroup projectsList, scenesList, packageList;

    [SerializeField]
    private CanvasGroup CanvasGroup;

    private List<SceneTile> sceneTiles = new List<SceneTile>();
    private List<ProjectTile> projectTiles = new List<ProjectTile>();
    private List<PackageTile> packageTiles = new List<PackageTile>();

    //filters
    private bool starredOnly = false;

    private void ShowSceneProjectManagerScreen(object sender, EventArgs args) {
        CanvasGroup.alpha = 1;
        CanvasGroup.blocksRaycasts = true;
    }

    private void HideSceneProjectManagerScreen(object sender, EventArgs args) {
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = false;
    }

    private void Start() {
        Base.GameManager.Instance.OnOpenMainScreen += ShowSceneProjectManagerScreen;
        Base.GameManager.Instance.OnOpenProjectEditor += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnOpenSceneEditor += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnDisconnectedFromServer += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnRunPackage += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnSceneListChanged += UpdateScenes;
        Base.GameManager.Instance.OnProjectsListChanged += UpdateProjects;
        Base.GameManager.Instance.OnPackagesListChanged += UpdatePackages;
        SwitchToScenes();
    }

    public void SwitchToProjects() {
        ScenesBtn.color = new Color(0.687f, 0.687f, 0.687f);
        PackagesBtn.color = new Color(0.687f, 0.687f, 0.687f);
        ProjectsBtn.color = new Color(0, 0, 0);
        projectsList.gameObject.SetActive(true);
        scenesList.gameObject.SetActive(false);
        packageList.gameObject.SetActive(false);
        FilterProjectsBySceneId(null);
        FilterLists();
    }

    public void SwitchToScenes() {
        ScenesBtn.color = new Color(0, 0, 0);
        ProjectsBtn.color = new Color(0.687f, 0.687f, 0.687f);
        PackagesBtn.color = new Color(0.687f, 0.687f, 0.687f);
        projectsList.gameObject.SetActive(false);
        packageList.gameObject.SetActive(false);
        scenesList.gameObject.SetActive(true);
        FilterScenesById(null);
        FilterLists();
    }

    public void SwitchToPackages() {
        PackagesBtn.color = new Color(0, 0, 0);
        ScenesBtn.color = new Color(0.687f, 0.687f, 0.687f);
        ProjectsBtn.color = new Color(0.687f, 0.687f, 0.687f);
        projectsList.gameObject.SetActive(false);
        scenesList.gameObject.SetActive(false);
        packageList.gameObject.SetActive(true);
        FilterLists();
    }

    public void FilterLists() {
        foreach (SceneTile tile in sceneTiles) {
            FilterTile(tile);
        }
        foreach (ProjectTile tile in projectTiles) {
            FilterTile(tile);
        }
        foreach (PackageTile tile in packageTiles) {
            FilterTile(tile);
        }
    }

    public void FilterTile(Tile tile) {
        if (starredOnly && !tile.GetStarred())
            tile.gameObject.SetActive(false);
        else
            tile.gameObject.SetActive(true);
    }

    public void FilterProjectsBySceneId(string sceneId) {
        foreach (ProjectTile tile in projectTiles) {
            if (sceneId == null) {
                tile.gameObject.SetActive(true);
                return;
            }               

            if (tile.SceneId != sceneId) {
                tile.gameObject.SetActive(false);
            }
        }
    }

    public void FilterScenesById(string sceneId) {
        foreach (SceneTile tile in sceneTiles) {
            if (sceneId == null) {
                tile.gameObject.SetActive(true);
                return;
            }

            if (tile.SceneId != sceneId) {
                tile.gameObject.SetActive(false);
            }
        }
    }

    public void ShowRelatedProjects(string sceneId) {
        SwitchToProjects();
        FilterProjectsBySceneId(sceneId);
    }

     public void ShowRelatedScene(string sceneId) {
        SwitchToScenes();
        FilterScenesById(sceneId);
    }

    public void EnableRecent(bool enable) {
        if (enable) {
            starredOnly = false;
            FilterLists();
        }            
    }

    public void EnableStarred(bool enable) {
        if (enable) {
            starredOnly = true;
            FilterLists();
        }
    }

    public void UpdateScenes(object sender, EventArgs eventArgs) {
        sceneTiles.Clear();
        foreach (Transform t in ScenesDynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (IO.Swagger.Model.ListScenesResponseData scene in Base.GameManager.Instance.Scenes) {
            SceneTile tile = Instantiate(SceneTilePrefab, ScenesDynamicContent.transform).GetComponent<SceneTile>();
            bool starred = PlayerPrefsHelper.LoadBool("scene/" + scene.Id + "/starred", false);
            tile.InitTile(scene.Name,
                          () => Base.GameManager.Instance.OpenScene(scene.Id),
                          () => SceneOptionMenu.Open(tile),
                          starred,
                          scene.Id,
                          scene.Modified.ToString());
            sceneTiles.Add(tile);
        }
        Button button = Instantiate(TileNewPrefab, ScenesDynamicContent.transform).GetComponent<Button>();
        // TODO new scene
        button.onClick.AddListener(ShowNewSceneDialog);
    }

    public async void NewScene(string name) {
        if (await Base.GameManager.Instance.NewScene(name)) {
            InputDialog.Close();
        }
    }

    public void ShowNewSceneDialog() {
        InputDialog.Open("Create new scene",
                         "Type new scene name",
                         "Name",
                         "",
                         () => NewScene(InputDialog.GetValue()),
                         () => InputDialog.Close());
    }

    public void UpdatePackages(object sender, EventArgs eventArgs) {
        packageTiles.Clear();
        foreach (Transform t in PackagesDynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (IO.Swagger.Model.PackageSummary package in Base.GameManager.Instance.Packages) {
            PackageTile tile = Instantiate(PackageTilePrefab, PackagesDynamicContent.transform).GetComponent<PackageTile>();
            bool starred = PlayerPrefsHelper.LoadBool("package/" + package.Id + "/starred", false);
            string projectName = "unknown";
            try {
                projectName = GameManager.Instance.GetProjectName(package.ProjectId);
            } catch (ItemNotFoundException ex) {
                Debug.Log(ex);
            }            
            tile.InitTile(package.PackageMeta.Name,
                          async () => await Base.GameManager.Instance.RunPackage(package.Id),
                          () => PackageOptionMenu.Open(tile),
                          starred,
                          package.Id,
                          projectName,
                          package.PackageMeta.Built.ToString());
            packageTiles.Add(tile);
        }
        
    }

    public void UpdateProjects(object sender, EventArgs eventArgs) {
        projectTiles.Clear();
        foreach (Transform t in ProjectsDynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (IO.Swagger.Model.ListProjectsResponseData project in Base.GameManager.Instance.Projects) {
            ProjectTile tile = Instantiate(ProjectTilePrefab, ProjectsDynamicContent.transform).GetComponent<ProjectTile>();
            bool starred = PlayerPrefsHelper.LoadBool("project/" + project.Id + "/starred", false);
            try {
                string sceneName = GameManager.Instance.GetSceneName(project.SceneId);
                tile.InitTile(project.Name,
                              () => GameManager.Instance.OpenProject(project.Id),
                              () => ProjectOptionMenu.Open(tile),
                              starred,
                              project.Id,
                              project.SceneId,
                              sceneName,
                              project.Modified.ToString());
                projectTiles.Add(tile);
            } catch (ItemNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs("Failed to load scene name.");
            }            
        }
        Button button = Instantiate(TileNewPrefab, ProjectsDynamicContent.transform).GetComponent<Button>();
        // TODO new scene
        button.onClick.AddListener(() => NewProjectDialog.Open());
    }

    public void NotImplemented() {
        Base.Notifications.Instance.ShowNotification("Not implemented", "Not implemented");
    }

    public void SaveLogs() {
        Base.Notifications.Instance.SaveLogs();
    }
}
