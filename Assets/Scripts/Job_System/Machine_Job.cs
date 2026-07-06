using System;

public enum Machine_Job_Type {
	Mill,
	Lathe,
	Laser
}

[Serializable]
public class Machine_Job {
	public Machine_Job_Type JobType;

	public Machine_Job(Machine_Job_Type jobType){
		JobType = jobType;
	}
}
