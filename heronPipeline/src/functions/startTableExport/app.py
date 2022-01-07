import os
from sys import exit, stderr
from datetime import datetime, timedelta
import boto3
from botocore.exceptions import ClientError
from botocore.config import Config
from boto3.dynamodb.conditions import Key


config = Config(
   retries = {
      'max_attempts': 10,
      'mode': 'standard'
   }
)

def lambda_handler(event, context):

	heronBucketName = os.getenv("HERON_BUCKET")
	heronMutationsTableArn = os.getenv("HERON_MUTATIONS_TABLE")

	print(f"Bucket: {heronBucketName}")
	print(f"Table: {heronMutationsTableArn}")

	yesterday = datetime.today() - timedelta(days=1)
	exportDate = datetime(yesterday.year, yesterday.month, yesterday.day, 0, 0, 0).timestamp()
	exportPartition = datetime.strftime(yesterday, "%Y-%m-%D")

  # Create a DynamoDB Client
	dynamodb = boto3.client('dynamodb', region_name="eu-west-1", config=config)
	ret = dynamodb.export_table_to_point_in_time(
    TableArn=heronMutationsTableArn,
    ExportTime=exportDate,
    S3Bucket=heronBucketName,
    S3Prefix=f"mutations/{exportPartition}",
    S3SseAlgorithm='AES256',
    ExportFormat='DYNAMODB_JSON'
  )
  
	print(f"Ret: {ret}")

	return "Hello World"