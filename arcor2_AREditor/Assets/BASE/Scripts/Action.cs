using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;

namespace Base {
    public abstract class Action : Clickable {
        // Metadata of this Action
        private ActionMetadata metadata;
        // Dictionary of all action parameters for this Action
        private Dictionary<string, ActionParameter> parameters = new Dictionary<string, ActionParameter>();
        
        public PuckInput Input;
        public PuckOutput Output;
        public IActionProvider ActionProvider;

        public ActionPoint ActionPoint;

        public IO.Swagger.Model.Action Data = new IO.Swagger.Model.Action("", new List<IO.Swagger.Model.ActionIO>(), new List<IO.Swagger.Model.ActionIO>(), new List<IO.Swagger.Model.ActionParameter>(), "", "");
        public async Task Init(string id, ActionMetadata metadata, ActionPoint ap, bool generateData, IActionProvider actionProvider, bool updateProject = true) {

            ActionPoint = ap;
            this.metadata = metadata;
            this.ActionProvider = actionProvider;

            if (generateData) {
                List<ActionParameter> dynamicParameters = new List<ActionParameter>();
                foreach (IO.Swagger.Model.ActionParameterMeta actionParameterMetadata in this.metadata.Parameters) {
                    
                    ActionParameter actionParameter = new ActionParameter(actionParameterMetadata, this);
                    switch (actionParameter.Type) {
                        case "relative_pose":
                            actionParameter.Value = Regex.Replace(new IO.Swagger.Model.Pose(orientation: new IO.Swagger.Model.Orientation(), position: new IO.Swagger.Model.Position()).ToJson(), @"\t|\n|\r", "");
                            break;
                        case "integer_enum":
                            actionParameter.Value = (int) actionParameterMetadata.AllowedValues[0];
                            break;
                        case "string_enum":
                            actionParameter.Value = (string) actionParameterMetadata.AllowedValues[0];
                            break;
                        case "pose":
                            List<string> poses = new List<string>(ap.GetPoses().Keys);
                            if (poses.Count == 0) {
                                actionParameter.Value = "";
                                //TODO: where to get valid ID?
                            } else {
                                actionParameter.Value = (string) ap.ActionObject.Data.Id + "." + ap.Data.Id + "." + poses[0];
                            }
                            break;
                        default:
                            actionParameter.Value = actionParameterMetadata.DefaultValue;
                            break;

                    }
                    if (actionParameterMetadata.DynamicValue) {
                        dynamicParameters.Add(actionParameter);
                    }

                    Parameters[actionParameter.ActionParameterMetadata.Name] = actionParameter;
                }
                foreach (InputOutput io in GetComponentsInChildren<InputOutput>()) {
                    io.InitData();
                }
                int parentCount = 0;
                while (dynamicParameters.Count > 0) {
                    for (int i = dynamicParameters.Count - 1; i >= 0; i--) {
                        ActionParameter actionParameter = dynamicParameters[i];
                        if (actionParameter.ActionParameterMetadata.DynamicValueParents.Count == parentCount) {
                            try {
                                List<IO.Swagger.Model.IdValue> args = new List<IO.Swagger.Model.IdValue>();
                                foreach (string parent in actionParameter.ActionParameterMetadata.DynamicValueParents) {
                                    string paramValue = "";
                                    if (parameters.TryGetValue(parent, out ActionParameter parameter)) {
                                        parameter.GetValue(out paramValue);
                                    } else {
                                        //TODO raise exception
                                    }
                                    args.Add(new IO.Swagger.Model.IdValue(parent, paramValue));
                                }
                                List<string> values = await actionParameter.LoadDynamicValues(args);
                                if (values.Count > 0) {
                                    actionParameter.Value = values[0];
                                } else {
                                    actionParameter.Value = "";
                                }
                            } catch (Exception ex) when (ex is ItemNotFoundException || ex is Base.RequestFailedException) {
                                Debug.LogError(ex);
                            } finally {
                                dynamicParameters.RemoveAt(i);
                            }
                        }
                    }
                    parentCount += 1;
                }
            }

            

            UpdateId(id, false);
            UpdateType();
            Data.Uuid = Guid.NewGuid().ToString();

            if (updateProject) {
                GameManager.Instance.UpdateProject();
            }


        }

        public void ActionUpdate(IO.Swagger.Model.Action aData = null) {
            if (aData != null)
                Data = aData;
        }



        public void UpdateType() {
            Data.Type = GetActionType();
        }

        

        public virtual void UpdateId(string newId, bool updateProject = true) {
            Data.Id = newId;
            if (updateProject)
                GameManager.Instance.UpdateProject();
        }

        public string GetActionType() {
            return ActionProvider.GetProviderName() + "/" + metadata.Name; //TODO: AO|Service/Id
        }

        public void DeleteAction(bool updateProject = true) {
            // Delete connection on input and set the gameobject that was connected through its output to the "end" value.
            if (Input.Connection != null) {
                InputOutput connectedActionIO = Input.Connection.target[0].GetComponent<InputOutput>();
                connectedActionIO.Data.Default = "end";
                // Remove the reference in connections manager.
                ConnectionManagerArcoro.Instance.Connections.Remove(Input.Connection);
                Destroy(Input.Connection.gameObject);
            }
            // Delete connection on output and set the gameobject that was connected through its input to the "start" value.
            if (Output.Connection != null) {
                InputOutput connectedActionIO = Output.Connection.target[1].GetComponent<InputOutput>();
                connectedActionIO.Data.Default = "start";
                // Remove the reference in connections manager.
                ConnectionManagerArcoro.Instance.Connections.Remove(Output.Connection);
                Destroy(Output.Connection.gameObject);
            }

            Destroy(gameObject);

            ActionPoint.Actions.Remove(Data.Id);

            if (updateProject)
                GameManager.Instance.UpdateProject();
        }

        public Dictionary<string, ActionParameter> Parameters {
            get => parameters; set => parameters = value;
        }
        public ActionMetadata Metadata {
            get => metadata; set => metadata = value;
        }

    }

}
