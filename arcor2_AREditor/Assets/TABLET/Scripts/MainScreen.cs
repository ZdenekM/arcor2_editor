using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MainScreen : MonoBehaviour
{
    List<string> scenes = new List<string>(), projects = new List<string>();
    public TMPro.TMP_Text ScenesBtn, ProjectsBtn;
    public GameObject SceneTilePrefab, TileNewPrefab, ProjectTilePrefab, ScenesDynamicContent, ProjectsDynamicContent;
    public NewSceneDialog NewSceneDialog;
    public NewProjectDialog NewProjectDialog;

    [SerializeField]
    private SceneOptionMenu SceneOptionMenu;

    [SerializeField]
    private CanvasGroup projectsList, scenesList;

    [SerializeField]
    private CanvasGroup CanvasGroup;

    private List<SceneTile> sceneTiles = new List<SceneTile>();

    private void ShowSceneProjectManagerScreen(object sender, EventArgs args) {
        CanvasGroup.alpha = 1;
    }

    private void HideSceneProjectManagerScreen(object sender, EventArgs args) {
        CanvasGroup.alpha = 0;
    }

    private void Start() {
        Base.GameManager.Instance.OnOpenMainScreen += ShowSceneProjectManagerScreen;
        Base.GameManager.Instance.OnOpenProjectEditor += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnOpenSceneEditor += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnDisconnectedFromServer += HideSceneProjectManagerScreen;
        Base.GameManager.Instance.OnSceneListChanged += UpdateScenes;
        Base.GameManager.Instance.OnProjectsListChanged += UpdateProjects;
        SwitchToScenes();
    }

    public void SwitchToProjects() {
        ScenesBtn.color = new Color(0.687f, 0.687f, 0.687f);
        ProjectsBtn.color = new Color(0, 0, 0);
        projectsList.alpha = 1;
        projectsList.blocksRaycasts = true;
        scenesList.alpha = 0;
        scenesList.blocksRaycasts = false;
    }

    public void SwitchToScenes() {
        ScenesBtn.color = new Color(0, 0, 0);
        ProjectsBtn.color = new Color(0.687f, 0.687f, 0.687f);
        projectsList.alpha = 0;
        projectsList.blocksRaycasts = false;
        scenesList.alpha = 1;
        scenesList.blocksRaycasts = true;
    }

    public void FilterLists(bool starred = false) {
        foreach (SceneTile sceneTile in sceneTiles) {
            if (starred && !sceneTile.GetStarred())
                sceneTile.gameObject.SetActive(false);
            else
                sceneTile.gameObject.SetActive(true);
        }
    }

    public void EnableRecent(bool enable) {
        if (enable)
            FilterLists(false);
    }

    public void EnableStarred(bool enable) {
        if (enable)
            FilterLists(true);
    }

    public void UpdateScenes(object sender, EventArgs eventArgs) {
        sceneTiles.Clear();
        foreach (Transform t in ScenesDynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (IO.Swagger.Model.IdDesc scene in Base.GameManager.Instance.Scenes) {
            SceneTile tile = Instantiate(SceneTilePrefab, ScenesDynamicContent.transform).GetComponent<SceneTile>();
            bool starred = Base.GameManager.Instance.LoadBool("scene/" + scene.Id + "/starred", false);
            tile.InitTile(scene.Id,
                          () => Base.GameManager.Instance.OpenScene(scene.Id),
                          () => SceneOptionMenu.Open(tile),
                          starred);
            sceneTiles.Add(tile);
        }
        Button button = Instantiate(TileNewPrefab, ScenesDynamicContent.transform).GetComponent<Button>();
        // TODO new scene
        button.onClick.AddListener(() => NewSceneDialog.WindowManager.OpenWindow());
    }

    public void UpdateProjects(object sender, EventArgs eventArgs) {
        foreach (Transform t in ProjectsDynamicContent.transform) {
            Destroy(t.gameObject);
        }
        foreach (IO.Swagger.Model.ListProjectsResponseData project in Base.GameManager.Instance.Projects) {
            SceneTile tile = Instantiate(SceneTilePrefab, ProjectsDynamicContent.transform).GetComponent<SceneTile>();
            tile.SetLabel(project.Id);
            tile.AddListener(() => Base.GameManager.Instance.OpenProject(project.Id));
        }
        Button button = Instantiate(TileNewPrefab, ProjectsDynamicContent.transform).GetComponent<Button>();
        // TODO new scene
        button.onClick.AddListener(() => NewProjectDialog.WindowManager.OpenWindow());
    }

    public void NotImplemented() {
        Base.Notifications.Instance.ShowNotification("Not implemented", "Not implemented");
    }
}
