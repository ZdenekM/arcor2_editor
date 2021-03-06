using Base;
using RuntimeGizmos;
using UnityEngine;
using System.Collections.Generic;
using IO.Swagger.Model;

public class ActionPoint3D : Base.ActionPoint {

    public GameObject Sphere, Visual, CollapsedPucksVisual;
    
    private bool manipulationStarted = false;
    private TransformGizmo tfGizmo;

    private float interval = 0.1f;
    private float nextUpdate = 0;

    private bool updatePosition = false;

    private OutlineOnClick outlineOnClick;

    protected override void Start() {
        base.Start();
        tfGizmo = Camera.main.GetComponent<TransformGizmo>();
        outlineOnClick = GetComponent<OutlineOnClick>();
    }

    protected override async void Update() {
        if (manipulationStarted) {
            if (tfGizmo.mainTargetRoot != null && GameObject.ReferenceEquals(tfGizmo.mainTargetRoot.gameObject, Sphere)) {
                if (!tfGizmo.isTransforming && updatePosition) {
                    updatePosition = false;
                    await GameManager.Instance.UpdateActionPointPosition(this, Data.Position);
                }

                if (tfGizmo.isTransforming)
                    updatePosition = true;


            } else {
                manipulationStarted = false;
            }
        }
            
        //TODO shouldn't this be called first?
        base.Update();
    }

    private void LateUpdate() {
        // Fix of AP rotations - works on both PC and tablet
        transform.rotation = Base.SceneManager.Instance.SceneOrigin.transform.rotation;
        if (Parent != null)
            orientations.transform.rotation = Parent.GetTransform().rotation;
        else
            orientations.transform.rotation = Base.SceneManager.Instance.SceneOrigin.transform.rotation;
    }

    public override void OnClick(Click type) {
        if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPoint) {
            GameManager.Instance.ObjectSelected(this);
            return;
        }
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal) {
            return;
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
            Notifications.Instance.ShowNotification("Not allowed", "Editation of action point only allowed in project editor");
            return;
        }
        // HANDLE MOUSE
        if (type == Click.MOUSE_LEFT_BUTTON) {
            StartManipulation();
        } else if (type == Click.MOUSE_RIGHT_BUTTON) {
            ShowMenu(false);
            tfGizmo.ClearTargets();
        }

        // HANDLE TOUCH
        else if (type == Click.TOUCH) {
            if (ControlBoxManager.Instance.UseGizmoMove || ControlBoxManager.Instance.UseGizmoRotate) {
                StartManipulation();
            } else {
                ShowMenu(false);
            }
        }
    }

    public void StartManipulation() {
        if (Locked) {
            Notifications.Instance.ShowNotification("Locked", "This action point is locked and can't be manipulated");
        } else {
            // We have clicked with left mouse and started manipulation with object
            Debug.LogWarning("Turning on gizmo overlay");
            manipulationStarted = true;
            updatePosition = false;
        }
    }

    public override Vector3 GetScenePosition() {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Position));
    }

    public override void SetScenePosition(Vector3 position) {
        Data.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation() {
        //return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Orientations[0].Orientation));
        return new Quaternion();
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        //Data.Orientations.Add(new IO.Swagger.Model.NamedOrientation(id: "default", orientation:DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation))));
    }

    public override void UpdatePositionsOfPucks() {
        CollapsedPucksVisual.SetActive(ProjectManager.Instance.AllowEdit && ActionsCollapsed);
        if (ProjectManager.Instance.AllowEdit && ActionsCollapsed) {
            foreach (Action3D action in Actions.Values) {
                action.transform.localPosition = new Vector3(0, 0, 0);
                action.transform.localScale = new Vector3(0, 0, 0);
            }
            
        } else {
            int i = 1;
            foreach (Action3D action in Actions.Values) {
                action.transform.localPosition = new Vector3(0, i * 0.1f, 0);
                ++i;
                action.transform.localScale = new Vector3(1, 1, 1);
            }
        }        
    }
    
    public override bool ProjectInteractable() {
        return base.ProjectInteractable() && !MenuManager.Instance.IsAnyMenuOpened;
    }

    public override void ActivateForGizmo(string layer) {
        if (!Locked) {
            base.ActivateForGizmo(layer);
            Sphere.layer = LayerMask.NameToLayer(layer);
        }
    }

    /// <summary>
    /// Changes size of shpere representing action point
    /// </summary>
    /// <param name="size"><0; 1> - 0 means invisble, 1 means 10cm in diameter</param>
    public override void SetSize(float size) {
        Visual.transform.localScale = new Vector3(size / 10, size / 10, size / 10);
    }

    public override (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ProjectActionPoint projectActionPoint) {
        (List<string>, Dictionary<string, string>) result = base.UpdateActionPoint(projectActionPoint);
        UpdateOrientationsVisuals();
        return result;
    }

    public override void UpdateOrientation(NamedOrientation orientation) {
        base.UpdateOrientation(orientation);
        UpdateOrientationsVisuals();
    }

    public override void AddOrientation(NamedOrientation orientation) {
        base.AddOrientation(orientation);
        UpdateOrientationsVisuals();
    }

    public override void HighlightAP(bool highlight) {
        if (highlight) {
            outlineOnClick.Highlight();
        } else {
            outlineOnClick.UnHighlight();
        }
    }

}
