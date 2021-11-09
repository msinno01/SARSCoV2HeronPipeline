using System.Collections;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.EFS;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.Python;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SQS;
using Stack = Amazon.CDK.Stack;
using Queue = Amazon.CDK.AWS.SQS.Queue;


namespace HeronPipeline {
  internal sealed class ExportResults: Construct {

    public EcsRunTask exportResultsTask;

    private Construct scope;
    
    private Infrastructure infrastructure;
    
    public ExportResults(Construct scope, string id, Infrastructure infrastructure): base(scope, id)
    {
      this.scope = scope;
      this.infrastructure = infrastructure;
    }
    public void Create()
    {
      var exportResultsImage = ContainerImage.FromAsset("src/images/exportResults");
      var exportResultsTaskDefinition = new TaskDefinition(this, "exportResultsTaskDefinition", new TaskDefinitionProps{
          Family = "exportResults",
          Cpu = "1024",
          MemoryMiB = "4096",
          NetworkMode = NetworkMode.AWS_VPC,
          Compatibility = Compatibility.FARGATE,
          ExecutionRole = infrastructure.ecsExecutionRole,
          TaskRole = infrastructure.ecsExecutionRole
      });
      exportResultsTaskDefinition.AddContainer("exportResultsContainer", new Amazon.CDK.AWS.ECS.ContainerDefinitionOptions
      {
          Image = exportResultsImage,
          Logging = new AwsLogDriver(new AwsLogDriverProps
          {
              StreamPrefix = "exportResults",
              LogGroup = new LogGroup(this, "exportResultsLogGroup", new LogGroupProps
              {
                  LogGroupName = "exportResultsLogGroup2",
                  Retention = RetentionDays.ONE_WEEK,
                  RemovalPolicy = RemovalPolicy.DESTROY
              })
          })
      });
      var exportResultsContainer = exportResultsTaskDefinition.FindContainer("exportResultsContainer");
      exportResultsTask = new EcsRunTask(this, "exportResultsTask", new EcsRunTaskProps
      {
          IntegrationPattern = IntegrationPattern.RUN_JOB,
          Cluster = infrastructure.cluster,
          TaskDefinition = exportResultsTaskDefinition,
          AssignPublicIp = true,
          LaunchTarget = new EcsFargateLaunchTarget(),
          ContainerOverrides = new ContainerOverride[] {
              new ContainerOverride {
                  ContainerDefinition = exportResultsContainer,
                  Environment = new TaskEnvironmentVariable[] {
                      new TaskEnvironmentVariable{
                        Name = "DATE_PARTITION",
                        Value = JsonPath.StringAt("$.date")
                      },
                      new TaskEnvironmentVariable{
                        Name = "HERON_SAMPLES_BUCKET",
                        Value = infrastructure.bucket.BucketName
                      },
                      new TaskEnvironmentVariable{
                          Name = "HERON_SEQUENCES_TABLE",
                          Value = infrastructure.sequencesTable.TableName
                      },
                      new TaskEnvironmentVariable{
                          Name = "HERON_SAMPLES_TABLE",
                          Value = infrastructure.samplesTable.TableName
                      },
                      new TaskEnvironmentVariable{
                        Name = "EXECUTION_ID",
                        Value = JsonPath.StringAt("$$.Execution.Id")
                      }
                  }
              }
          },
          ResultPath = "$.result"
      });
    }
  }
}